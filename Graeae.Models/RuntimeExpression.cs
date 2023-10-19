using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Pointer;

namespace Graeae.Models;

/// <summary>
/// Models an OpenAPI runtime expression.
/// </summary>
// TODO: maybe in the future build some factory methods.
public class RuntimeExpression : IEquatable<string>, IEquatable<RuntimeExpression>
{
	// see https://spec.openapis.org/oas/v3.1.0#runtime-expressions

	// expression = ( "$url" / "$method" / "$statusCode" / "$request." source / "$response." source )
	// source = (header-reference / query-reference / path-reference / body-reference )
	// header-reference = "header." token
	// query-reference = "query." name
	// path-reference = "path." name
	// body-reference = "body" ["#" json-pointer]
	// json-pointer    = *( "/" reference-token )
	// reference-token = *(unescaped / escaped )
	// unescaped       = %x00-2E / %x30-7D / %x7F-10FFFF
	//      ; %x2F('/') and %x7E('~') are excluded from 'unescaped'
	// escaped         = "~" ( "0" / "1" )
	//      ; representing '~' and '/', respectively
	// name = *(CHAR)
	// token = 1 * tchar
	// tchar = "!" / "#" / "$" / "%" / "&" / "'" / "*" / "+" / "-" / "." /
	//         "^" / "_" / "`" / "|" / "~" / DIGIT / ALPHA

	private const string TokenSymbols = "!#$&'*+-.^_`|~";

	private string _source;

	/// <summary>
	/// A `$url` runtime expression.
	/// </summary>
	public static readonly RuntimeExpression Url = new() { ExpressionType = RuntimeExpressionType.Url, _source = "$url" };

	/// <summary>
	/// A `$method` runtime expression.
	/// </summary>
	public static readonly RuntimeExpression Method = new() { ExpressionType = RuntimeExpressionType.Method, _source = "$method" };

	/// <summary>
	/// A `$statusCode` runtime expression.
	/// </summary>
	public static readonly RuntimeExpression StatusCode = new() { ExpressionType = RuntimeExpressionType.StatusCode, _source = "$statusCode" };

	/// <summary>
	/// Gets the expression type.
	/// </summary>
	public RuntimeExpressionType ExpressionType { get; private set; }
	/// <summary>
	/// Gets the source type.
	/// </summary>
	public RuntimeExpressionSourceType? SourceType { get; private set; }
	/// <summary>
	/// Gets the token.
	/// </summary>
	public string? Token { get; private set; }
	/// <summary>
	/// Gets the name.
	/// </summary>
	public string? Name { get; private set; }
	/// <summary>
	/// Gets the JSON Pointer.
	/// </summary>
	public JsonPointer? JsonPointer { get; private set; }

#pragma warning disable CS8618
	private RuntimeExpression(){}
#pragma warning restore CS8618

	internal static RuntimeExpression FromNode(JsonNode? node)
	{
		if (node is not JsonValue value || !value.TryGetValue(out string? source))
			throw new JsonException("runtime expressions must be strings");

		return Parse(source);
	}

	/// <summary>
	/// Parses a runtime expression from a string.
	/// </summary>
	/// <param name="source">The string source</param>
	/// <returns>A runtime expression</returns>
	/// <exception cref="JsonException">Throw when the parse fails</exception>
	public static RuntimeExpression Parse(string source)
	{
		var expr = new RuntimeExpression{_source = source};
		var i = 0;

		source.Expect(ref i, "$");
		var expression = source.Expect(ref i, "url", "method", "statusCode", "request", "response");
		switch (expression)
		{
			case "url":
				return Url;
			case "method":
				return Method;
			case "statusCode":
				return StatusCode;
			case "request":
				expr.ExpressionType = RuntimeExpressionType.Request;
				break;
			case "response":
				expr.ExpressionType = RuntimeExpressionType.Response;
				break;
		}

		source.Expect(ref i, ".");
		var sourceType = source.Expect(ref i, "header", "query", "path", "body");
		switch (sourceType)
		{
			case "header":
				expr.SourceType = RuntimeExpressionSourceType.Header;
				source.Expect(ref i, ".");
				var j = i;
				while (j < source.Length && (char.IsLetterOrDigit(source[j]) || TokenSymbols.Contains(source[j])))
				{
					j++;
				}
				expr.Token = source.Substring(i, j-i);
				break;
			case "query":
				expr.SourceType = RuntimeExpressionSourceType.Query;
				source.Expect(ref i, ".");
				expr.Name = source.Substring(i);
				break;
			case "path":
				expr.SourceType = RuntimeExpressionSourceType.Path;
				source.Expect(ref i, ".");
				expr.Name = source.Substring(i);
				break;
			case "body":
				expr.SourceType = RuntimeExpressionSourceType.Body;
				source.Expect(ref i, "#");
				if (i < source.Length)
				{
					if (JsonPointer.TryParse(source.Substring(i), out var jp))
						expr.JsonPointer = jp;
					else
						throw new JsonException("Text after `#` must be a valid JSON Pointer");
				}

				break;
		}

		return expr;
	}

	/// <summary>Returns a string that represents the current object.</summary>
	/// <returns>A string that represents the current object.</returns>
	public override string ToString()
	{
		return _source;
	}

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>
	/// <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.</returns>
	public bool Equals(string? other)
	{
		return other == _source;
	}

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>
	/// <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.</returns>
	public bool Equals(RuntimeExpression? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;

		return _source == other._source;
	}

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>
	/// <see langword="true" /> if the specified object  is equal to the current object; otherwise, <see langword="false" />.</returns>
	public override bool Equals(object? obj)
	{
		return Equals(obj as RuntimeExpression);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		return _source.GetHashCode();
	}
}
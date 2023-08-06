using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Pointer;

namespace OpenApi.Models;

/// <summary>
/// Models an OpenAPI runtime expression.
/// </summary>
// TODO: maybe in the future build some factory methods.
public class RuntimeExpression : IEquatable<string>
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
				expr.ExpressionType = RuntimeExpressionType.Url;
				return expr;
			case "method":
				expr.ExpressionType = RuntimeExpressionType.Method;
				return expr;
			case "statusCode":
				expr.ExpressionType = RuntimeExpressionType.StatusCode;
				return expr;
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
				expr.Token = source[i..j];
				break;
			case "query":
				expr.SourceType = RuntimeExpressionSourceType.Query;
				source.Expect(ref i, ".");
				expr.Name = source[i..];
				break;
			case "path":
				expr.SourceType = RuntimeExpressionSourceType.Path;
				source.Expect(ref i, ".");
				expr.Name = source[i..];
				break;
			case "body":
				expr.SourceType = RuntimeExpressionSourceType.Body;
				source.Expect(ref i, "#");
				if (i < source.Length)
				{
					if (JsonPointer.TryParse(source[i..], out var jp))
						expr.JsonPointer = jp;
					else
						throw new JsonException("Text after `#` must be a valid JSON Pointer");
				}

				break;
		}

		return expr;
	}

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>
	/// <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.</returns>
	public bool Equals(string? other)
	{
		return other == _source;
	}

	/// <summary>Returns a string that represents the current object.</summary>
	/// <returns>A string that represents the current object.</returns>
	public override string ToString()
	{
		return _source;
	}
}
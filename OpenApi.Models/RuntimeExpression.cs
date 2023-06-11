using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Pointer;

namespace OpenApi.Models;

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

	private static string TokenSymbols = "!#$&'*+-.^_`|~";

	private string _source;

	public RuntimeExpressionType ExpressionType { get; set; }
	public RuntimeExpressionSourceType? SourceType { get; set; }
	public string? Token { get; set; }
	public string? Name { get; set; }
	public JsonPointer? JsonPointer { get; set; }

#pragma warning disable CS8618
	private RuntimeExpression(){}
#pragma warning restore CS8618

	public static RuntimeExpression FromNode(JsonNode? node)
	{
		if (node is not JsonValue value || !value.TryGetValue(out string? source))
			throw new JsonException("runtime expressions must be strings");

		return Parse(source);
	}

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

	public bool Equals(string? other)
	{
		return other == _source;
	}

	public override string ToString()
	{
		// TODO: need generation logic for this
		return _source;
	}
}
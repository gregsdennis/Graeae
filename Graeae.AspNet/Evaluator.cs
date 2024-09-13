using System.Text;
using System.Text.Json.Nodes;
using Graeae.Models;
using Json.More;
using Json.Pointer;
using Json.Schema;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Graeae.AspNet;

/// <summary>
/// Extensions to evaluate runtime expressions against an HTTP call.
/// </summary>
public static class Evaluator
{
	/// <summary>
	/// Resolves a callback key expression.
	/// </summary>
	/// <param name="expr">The callback key expression</param>
	/// <param name="context">The HTTP context</param>
	/// <param name="pathTemplate">(optional) A path template</param>
	/// <returns>The resolved expression as a URI</returns>
	public static Uri Resolve(this CallbackKeyExpression expr, HttpContext context, PathTemplate? pathTemplate = null)
	{
		var sb = new StringBuilder(expr.Source);

		foreach (var parameter in expr.Parameters)
		{
			var value = Resolve(parameter, context, pathTemplate);
			sb.Replace(parameter.ToString(), value);
		}

		return new Uri(sb.ToString());
	}

	/// <summary>
	/// Resolves a runtime expression.
	/// </summary>
	/// <param name="expr">The runtime expression</param>
	/// <param name="context">The HTTP context</param>
	/// <param name="pathTemplate">(optional) A path template</param>
	/// <returns>The resolved expression</returns>
	public static string? Resolve(this RuntimeExpression expr, HttpContext context, PathTemplate? pathTemplate = null)
	{
		return expr.ExpressionType switch
		{
			RuntimeExpressionType.Url => context.Request.GetEncodedUrl(),
			RuntimeExpressionType.Method => context.Request.Method,
			RuntimeExpressionType.StatusCode => context.Response.StatusCode.ToString(),
			RuntimeExpressionType.Request => GetFromRequest(expr, context.Request, pathTemplate),
			RuntimeExpressionType.Response => GetFromResponse(expr, context.Response),
			_ => throw new ArgumentOutOfRangeException(nameof(expr))
		};
	}

	private static string? GetFromRequest(RuntimeExpression expr, HttpRequest request, PathTemplate? pathTemplate)
	{
		switch (expr.SourceType)
		{
			case RuntimeExpressionSourceType.Header:
				request.Headers.TryGetValue(expr.Token!, out var header);
				return header;
			case RuntimeExpressionSourceType.Query:
				request.Query.TryGetValue(expr.Name!, out var queryParam);
				return queryParam;
			case RuntimeExpressionSourceType.Path:
				ArgumentNullException.ThrowIfNull(pathTemplate);
				var actualPath = PathTemplate.Parse(request.Path.Value!);
				var matched = pathTemplate.Segments.Zip(actualPath.Segments, (x, y) => (Template: x, Actual: y));
				var target = $"{{{expr.Name}}}";
				return matched.FirstOrDefault(x => x.Template == target).Actual;
			case RuntimeExpressionSourceType.Body:
				return GetFromStream(expr.JsonPointer!, request.Body);
			case null:
			case RuntimeExpressionSourceType.Unspecified:
			default:
				throw new ArgumentOutOfRangeException(nameof(expr));
		}
	}

	private static string GetFromResponse(RuntimeExpression expr, HttpResponse response)
	{
		switch (expr.SourceType)
		{
			case RuntimeExpressionSourceType.Header:
				response.Headers.TryGetValue(expr.Token!, out var header);
				return header!;
			case RuntimeExpressionSourceType.Query:
				throw new NotSupportedException("$response.query is not supported");
			case RuntimeExpressionSourceType.Path:
				throw new NotSupportedException("$response.path is not supported");
			case RuntimeExpressionSourceType.Body:
				return GetFromStream(expr.JsonPointer!, response.Body);
			case null:
			case RuntimeExpressionSourceType.Unspecified:
			default:
				throw new ArgumentOutOfRangeException(nameof(expr));
		}
	}

	private static string GetFromStream(JsonPointer pointer, Stream stream)
	{
		using var newStream = new MemoryStream();

		var position = stream.Position;
		stream.Position = 0;
		stream.CopyTo(newStream);
		stream.Position = position;
		newStream.Position = 0;

		using var reader = new StreamReader(newStream);
		var content = reader.ReadToEnd();

		var json = JsonNode.Parse(content);

		pointer.TryEvaluate(json, out var target);
		return target.GetSchemaValueType() == SchemaValueType.String
			? target!.GetValue<string>()
			: target.AsJsonString();
	}
}
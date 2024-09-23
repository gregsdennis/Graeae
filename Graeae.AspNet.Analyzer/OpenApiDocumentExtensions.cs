using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Corvus.Json;
using Graeae.Models;
using Json.Schema;
using JsonPointer = Json.Pointer.JsonPointer;

namespace Graeae.AspNet.Analyzer;

internal static class OpenApiDocumentExtensions
{
	public static IEnumerable<JsonReference> FindSchemaLocations(this OpenApiDocument openApiDocument, string documentPath)
	{
		return GetSchemas(JsonPointer.Create("components"), openApiDocument.Components)
			.Concat(GetSchemas(JsonPointer.Create("paths"), openApiDocument.Paths))
			.Concat(GetSchemas(JsonPointer.Create("webhooks"), openApiDocument.Webhooks))
			.Select(x => new JsonReference(new Uri(documentPath).ToString(), $"#{x}"));
	}

	private static IEnumerable<JsonPointer> GetSchemas(JsonPointer baseRoute, ComponentCollection? components)
	{
		if (components is null) return [];

		return GetSchemas(baseRoute.Combine("schemas"), components.Schemas)
			.Concat(GetSchemas(baseRoute.Combine("responses"), components.Responses))
			.Concat(GetSchemas(baseRoute.Combine("parameters"), components.Parameters))
			.Concat(GetSchemas(baseRoute.Combine("requestBodies"), components.RequestBodies))
			.Concat(GetSchemas(baseRoute.Combine("headers"), components.Headers))
			.Concat(GetSchemas(baseRoute.Combine("callbacks"), components.Callbacks))
			.Concat(GetSchemas(baseRoute.Combine("pathItems"), components.PathItems));
	}

	private static IEnumerable<JsonPointer> GetSchemas(JsonPointer baseRoute, IDictionary<string, JsonSchema>? schemas)
	{
		if (schemas is null) return [];

		return schemas.Select(x => baseRoute.Combine(x.Key));
	}

	private static IEnumerable<JsonPointer> GetSchemas<T>(JsonPointer baseRoute, IDictionary<T, Response>? responses)
	{
		if (responses is null) return [];

		return responses.SelectMany(x =>
		{
			var b = baseRoute.Combine(x.Key.GetKeyString());
			return GetSchemas(b.Combine("headers"), x.Value.Headers)
				.Concat(GetSchemas(b.Combine("content"), x.Value.Content));
		});
	}

	private static IEnumerable<JsonPointer> GetSchemas(JsonPointer baseRoute, IDictionary<string, Models.Parameter>? parameters)
	{
		if (parameters is null) return [];

		return parameters.SelectMany(x => GetSchemas(baseRoute.Combine(x.Key, "content"), x.Value.Content));
	}

	private static IEnumerable<JsonPointer> GetSchemas(JsonPointer baseRoute, IReadOnlyList<Models.Parameter>? parameters)
	{
		if (parameters is null) return [];

		return parameters.SelectMany((x, i) => GetSchemas(baseRoute.Combine(i, "content"), x.Content));
	}

	private static IEnumerable<JsonPointer> GetSchemas(JsonPointer baseRoute, IDictionary<string, RequestBody>? requestBodies)
	{
		if (requestBodies is null) return [];

		return requestBodies.SelectMany(x => GetSchemas(baseRoute.Combine(x.Key, "content"), x.Value.Content));
	}

	private static IEnumerable<JsonPointer> GetSchemas(JsonPointer baseRoute, IDictionary<string, Header>? headers)
	{
		if (headers is null) return [];

		return headers.SelectMany(x => GetSchemas(baseRoute.Combine(x.Key, "content"), x.Value.Content));
	}

	private static IEnumerable<JsonPointer> GetSchemas(JsonPointer baseRoute, IDictionary<string, Callback>? callbacks)
	{
		if (callbacks is null) return [];

		return callbacks.SelectMany(x => GetSchemas(baseRoute.Combine(x.Key), x.Value));
	}

	private static IEnumerable<JsonPointer> GetSchemas(JsonPointer baseRoute, IDictionary<string, MediaType>? mediaTypes)
	{
		if (mediaTypes is null) return [];

		return mediaTypes.Select(x => baseRoute.Combine(x.Key, "schema"));
	}

	private static IEnumerable<JsonPointer> GetSchemas<T>(JsonPointer baseRoute, IDictionary<T, PathItem>? pathItems)
	{
		if (pathItems is null) return [];

		return pathItems.SelectMany(x =>
		{
			var b = baseRoute.Combine(x.Key.GetKeyString());
			return GetSchemas(b.Combine("get"), x.Value.Get)
				.Concat(GetSchemas(b.Combine("put"), x.Value.Put))
				.Concat(GetSchemas(b.Combine("post"), x.Value.Post))
				.Concat(GetSchemas(b.Combine("delete"), x.Value.Delete))
				.Concat(GetSchemas(b.Combine("options"), x.Value.Options))
				.Concat(GetSchemas(b.Combine("head"), x.Value.Head))
				.Concat(GetSchemas(b.Combine("patch"), x.Value.Patch))
				.Concat(GetSchemas(b.Combine("trace"), x.Value.Trace))
				.Concat(GetSchemas(b.Combine("parameters"), x.Value.Parameters));
		});
	}

	private static IEnumerable<JsonPointer> GetSchemas(JsonPointer baseRoute, Operation? operation)
	{
		if (operation is null) return [];

		return GetSchemas(baseRoute.Combine("parameters"), operation.Parameters)
			.Concat(GetSchemas(baseRoute.Combine("requestBody", "content"), operation.RequestBody?.Content))
			.Concat(GetSchemas(baseRoute.Combine("responses"), operation.Responses))
			.Concat(GetSchemas(baseRoute.Combine("callbacks"), operation.Callbacks));
	}

	private static string GetKeyString<T>(this T? value)
	{
		var keyString = typeof(T).IsEnum
			? Convert.ToInt32(value).ToString()
			: value!.ToString();

		return keyString;
	}
}
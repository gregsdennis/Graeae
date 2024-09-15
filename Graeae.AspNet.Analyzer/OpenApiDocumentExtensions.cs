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
	public static IEnumerable<(JsonReference Ref, JsonDocument Schema)> FindSchemaLocations(this OpenApiDocument openApiDocument, string documentPath)
	{
		return GetSchemas(JsonPointer.Create("components"), openApiDocument.Components)
			.Concat(GetSchemas(JsonPointer.Create("paths"), openApiDocument.Paths))
			.Concat(GetSchemas(JsonPointer.Create("webhooks"), openApiDocument.Webhooks))
			.Where(x => x.Item2 is not null)
			.Select(x => (new JsonReference(documentPath, $"#{x.Item1}"), JsonSerializer.SerializeToDocument(x.Item2)));
	}

	private static IEnumerable<(JsonPointer, JsonSchema?)> GetSchemas(JsonPointer baseRoute, ComponentCollection? components)
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

	private static IEnumerable<(JsonPointer, JsonSchema?)> GetSchemas(JsonPointer baseRoute, IDictionary<string, JsonSchema>? schemas)
	{
		if (schemas is null) return [];

		return schemas.Select(x => (baseRoute.Combine(x.Key), x.Value))!;
	}

	private static IEnumerable<(JsonPointer, JsonSchema?)> GetSchemas<T>(JsonPointer baseRoute, IDictionary<T, Response>? responses)
	{
		if (responses is null) return [];

		return responses.SelectMany(x =>
		{
			var b = baseRoute.Combine(x.Key!.ToString());
			return GetSchemas(b, x.Value.Headers)
				.Concat(GetSchemas(b, x.Value.Content));
		});
	}

	private static IEnumerable<(JsonPointer, JsonSchema?)> GetSchemas(JsonPointer baseRoute, IDictionary<string, Models.Parameter>? parameters)
	{
		if (parameters is null) return [];

		return parameters.SelectMany(x =>
		{
			var b = baseRoute.Combine(x.Key);
			return GetSchemas(b, x.Value.Content);
		});
	}

	private static IEnumerable<(JsonPointer, JsonSchema?)> GetSchemas(JsonPointer baseRoute, IReadOnlyList<Models.Parameter>? parameters)
	{
		if (parameters is null) return [];

		return parameters.SelectMany((x, i) =>
		{
			var b = baseRoute.Combine(i);
			return GetSchemas(b, x.Content);
		});
	}

	private static IEnumerable<(JsonPointer, JsonSchema?)> GetSchemas(JsonPointer baseRoute, IDictionary<string, RequestBody>? requestBodies)
	{
		if (requestBodies is null) return [];

		return requestBodies.SelectMany(x =>
		{
			var b = baseRoute.Combine(x.Key);
			return GetSchemas(b, x.Value.Content);
		});
	}

	private static IEnumerable<(JsonPointer, JsonSchema?)> GetSchemas(JsonPointer baseRoute, IDictionary<string, Header>? headers)
	{
		if (headers is null) return [];

		return headers.SelectMany(x =>
		{
			var b = baseRoute.Combine(x.Key);
			return GetSchemas(b, x.Value.Content);
		});
	}

	private static IEnumerable<(JsonPointer, JsonSchema?)> GetSchemas(JsonPointer baseRoute, IDictionary<string, Callback>? callbacks)
	{
		if (callbacks is null) return [];

		return callbacks.SelectMany(x =>
		{
			var b = baseRoute.Combine(x.Key);
			return GetSchemas(b, x.Value);
		});
	}

	private static IEnumerable<(JsonPointer, JsonSchema?)> GetSchemas(JsonPointer baseRoute, IDictionary<string, MediaType>? mediaTypes)
	{
		if (mediaTypes is null) return [];

		return mediaTypes.Select(x => (baseRoute.Combine(x.Key, "schema"), x.Value.Schema));
	}

	private static IEnumerable<(JsonPointer, JsonSchema?)> GetSchemas<T>(JsonPointer baseRoute, IDictionary<T, PathItem>? pathItems)
	{
		if (pathItems is null) return [];

		return pathItems.SelectMany(x =>
		{
			var b = baseRoute.Combine(x.Key!.ToString());
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

	private static IEnumerable<(JsonPointer, JsonSchema?)> GetSchemas(JsonPointer baseRoute, Operation? operation)
	{
		if (operation is null) return [];

		return GetSchemas(baseRoute.Combine("parameters"), operation.Parameters)
			.Concat(GetSchemas(baseRoute.Combine("requestBody"), operation.RequestBody?.Content))
			.Concat(GetSchemas(baseRoute.Combine("responses"), operation.Responses))
			.Concat(GetSchemas(baseRoute.Combine("callbacks"), operation.Callbacks));
	}
}
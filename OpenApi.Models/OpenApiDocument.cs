using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Pointer;
using Json.Schema;

namespace OpenApi.Models;

[JsonConverter(typeof(OpenApiDocumentJsonConverter))]
public class OpenApiDocument : IBaseDocument
{
	private static readonly string[] KnownKeys =
	{
		"openapi",
		"info",
		"jsonSchemaDialect",
		"servers",
		"paths",
		"webhooks",
		"components",
		"security",
		"tags",
		"externalDocs"
	};

	private readonly Dictionary<JsonPointer, object> _lookup = new();

	public string OpenApi { get; set; }
	public OpenApiInfo Info { get; set; }
	public Uri? JsonSchemaDialect { get; set; }
	public IEnumerable<Server>? Servers { get; set; }
	public PathCollection? Paths { get; set; }
	public Dictionary<string, PathItem>? Webhooks { get; set; }
	public ComponentCollection? Components { get; set; }
	public IEnumerable<SecurityRequirement>? Security { get; set; }
	public IEnumerable<Tag>? Tags { get; set; }
	public ExternalDocumentation? ExternalDocs { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	Uri IBaseDocument.BaseUri { get; }

	public static OpenApiDocument FromNode(JsonNode? node, JsonSerializerOptions? options)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var document = new OpenApiDocument
		{
			OpenApi = obj.ExpectString("openapi", "open api document"),
			Info = obj.Expect("info", "open api document", OpenApiInfo.FromNode),
			JsonSchemaDialect = obj.MaybeUri("jsonSchemaDialect", "open api document"),
			Servers = obj.MaybeArray("servers", Server.FromNode),
			Paths = obj.Maybe("paths", x => PathCollection.FromNode(x, options)),
			Webhooks = obj.MaybeMap("webhooks", x => PathItem.FromNode(x, options)),
			Components = obj.Maybe("components", x => ComponentCollection.FromNode(x, options)),
			Security = obj.MaybeArray("security", SecurityRequirement.FromNode),
			Tags = obj.MaybeArray("tags", Tag.FromNode),
			ExternalDocs = obj.Maybe("externalDocs", ExternalDocumentation.FromNode),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, document.ExtensionData?.Keys);

		return document;
	}

	public static JsonNode? ToNode(OpenApiDocument? document, JsonSerializerOptions? options)
	{
		if (document == null) return null;

		var obj = new JsonObject
		{
			["openapi"] = document.OpenApi,
			["info"] = OpenApiInfo.ToNode(document.Info)
		};

		obj.MaybeAdd("jsonSchemaDialect", document.JsonSchemaDialect?.ToString());
		obj.MaybeAddArray("servers", document.Servers, x => Server.ToNode(x, options));
		obj.MaybeAdd("paths", PathCollection.ToNode(document.Paths, options));
		obj.MaybeAddMap("webhooks", document.Webhooks, x => PathItem.ToNode(x, options));
		obj.MaybeAdd("components", ComponentCollection.ToNode(document.Components, options));
		obj.MaybeAddArray("security", document.Security, x => SecurityRequirement.ToNode(x, options));
		obj.MaybeAddArray("tags", document.Tags, Tag.ToNode);
		obj.MaybeAdd("externalDocs", ExternalDocumentation.ToNode(document.ExternalDocs));
		obj.AddExtensions(document.ExtensionData);

		return obj;
	}

	JsonSchema? IBaseDocument.FindSubschema(JsonPointer pointer, EvaluationOptions options)
	{
		return !TryFind(pointer, out JsonSchema? schema) ? null : schema;
	}

	public bool TryFind<T>(JsonPointer pointer, out T? value)
		where T : class
	{
		if (!_lookup.TryGetValue(pointer, out var val))
		{
			var keys = pointer.Segments.Select(x => x.Value).ToArray();

			val = PerformLookup(keys) as T;
			if (val != null)
				_lookup[pointer] = val;
		}

		value = val as T;
		return value != null;
	}

	private object? PerformLookup(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		int keysConsumed = 1;
		IRefResolvable? target = null;
		switch (keys[0])
		{
			case "info":
				target = Info;
				break;
			case "servers":
				if (keys.Length == 1) return null;
				keysConsumed++;
				target = Servers?.GetFromArray(keys[1]);
				break;
			case "paths":
				target = Paths;
				break;
			case "webhooks":
				if (keys.Length == 1) return null;
				keysConsumed++;
				target = Webhooks?.GetFromMap(keys[1]);
				break;
			case "components":
				target = Components;
				break;
			case "tags":
				if (keys.Length == 1) return null;
				keysConsumed++;
				target = Tags?.GetFromArray(keys[1]);
				break;
			case "externalDocs":
				target = ExternalDocs;
				break;
		}

		return target != null
			? target.Resolve(keys[keysConsumed..])
			: ExtensionData?.Resolve(keys);
	}
}

public class OpenApiDocumentJsonConverter : JsonConverter<OpenApiDocument>
{
	public override OpenApiDocument? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return OpenApiDocument.FromNode(obj, options);
	}

	public override void Write(Utf8JsonWriter writer, OpenApiDocument value, JsonSerializerOptions options)
	{
		var json = OpenApiDocument.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}
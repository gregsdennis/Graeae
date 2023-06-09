using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Pointer;
using Json.Schema;
using OpenApi.Models.Draft4;
using Vocabularies = Json.Schema.OpenApi.Vocabularies;

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

	public string OpenApi { get; }
	public OpenApiInfo Info { get; }
	public Uri? JsonSchemaDialect { get; set; }
	public IEnumerable<Server>? Servers { get; set; }
	public PathCollection? Paths { get; set; }
	public Dictionary<string, PathItem>? Webhooks { get; set; }
	public ComponentCollection? Components { get; set; }
	public IEnumerable<SecurityRequirement>? Security { get; set; }
	public IEnumerable<Tag>? Tags { get; set; }
	public ExternalDocumentation? ExternalDocs { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	Uri IBaseDocument.BaseUri { get; } = GenerateBaseUri();

	// TODO: Change this base URI to something appropriate for this library.
	private static Uri GenerateBaseUri() => new($"https://json-everything.net/{Guid.NewGuid().ToString("N")[..10]}");

	static OpenApiDocument()
	{
		Json.Schema.Formats.Register(Formats.Double);
		Json.Schema.Formats.Register(Formats.Float);
		Json.Schema.Formats.Register(Formats.Int32);
		Json.Schema.Formats.Register(Formats.Int64);
		Json.Schema.Formats.Register(Formats.Password);

		VocabularyRegistry.Global.Register(Vocabularies.OpenApi);
	}

	public OpenApiDocument(string openApi, OpenApiInfo info)
	{
		OpenApi = openApi;
		Info = info;
	}

	public static void EnableLegacySupport()
	{
		SchemaKeywordRegistry.Register<Draft4ExclusiveMaximumKeyword>();
		SchemaKeywordRegistry.Register<Draft4ExclusiveMinimumKeyword>();
		SchemaKeywordRegistry.Register<Draft4IdKeyword>();
		SchemaKeywordRegistry.Register<NullableKeyword>();

		SchemaRegistry.Global.Register(Draft4Support.Draft4MetaSchema);
	}

	JsonSchema? IBaseDocument.FindSubschema(JsonPointer pointer, EvaluationOptions options)
	{
		return Find<JsonSchema>(pointer);
	}

	public static OpenApiDocument FromNode(JsonNode? node, JsonSerializerOptions? options)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var document = new OpenApiDocument(
			obj.ExpectString("openapi", "open api document"),
			obj.Expect("info", "open api document", OpenApiInfo.FromNode))
		{
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
		obj.MaybeAddArray("servers", document.Servers, Server.ToNode);
		obj.MaybeAdd("paths", PathCollection.ToNode(document.Paths, options));
		obj.MaybeAddMap("webhooks", document.Webhooks, x => PathItem.ToNode(x, options));
		obj.MaybeAdd("components", ComponentCollection.ToNode(document.Components, options));
		obj.MaybeAddArray("security", document.Security, SecurityRequirement.ToNode);
		obj.MaybeAddArray("tags", document.Tags, Tag.ToNode);
		obj.MaybeAdd("externalDocs", ExternalDocumentation.ToNode(document.ExternalDocs));
		obj.AddExtensions(document.ExtensionData);

		return obj;
	}

	public void Initialize(SchemaRegistry? schemaRegistry = null)
	{
		schemaRegistry ??= SchemaRegistry.Global;

		schemaRegistry.Register(this);

		// find all JSON Schemas and populate their base URIs (if they don't have $id)
		RegisterSchemas(schemaRegistry);

		// find and attempt to resolve all reference objects
		TryResolveRefs();
	}

	private void RegisterSchemas(SchemaRegistry schemaRegistry)
	{
		var allSchemas = GeneralHelpers.Collect(
			Paths?.FindSchemas(),
			Webhooks?.Values.SelectMany(x => x.FindSchemas()),
			Components?.FindSchemas()
		);

		var baseUri = ((IBaseDocument)this).BaseUri;
		foreach (var schema in allSchemas)
		{
			if (schema.BoolValue.HasValue) continue;
			if (schema.Keywords!.OfType<IdKeyword>().Any())
				schemaRegistry.Register(schema);

			schema.BaseUri = baseUri;
		}
	}

	private void TryResolveRefs()
	{
		var allRefs = GeneralHelpers.Collect(
			Paths?.FindRefs(),
			Webhooks?.Values.SelectMany(x => x.FindRefs()),
			Components?.FindRefs()
		);

		foreach (var reference in allRefs)
		{
			reference.Resolve(this);
		}
	}

	public T? Find<T>(JsonPointer pointer)
		where T : class
	{
		if (!_lookup.TryGetValue(pointer, out var val))
		{
			var keys = pointer.Segments.Select(x => x.Value).ToArray();

			val = PerformLookup(keys) as T;
			if (val != null)
				_lookup[pointer] = val;
		}

		if (val is JsonNode node && typeof(T) != typeof(JsonNode))
			return node.Deserialize<T>();

		return val as T;
	}

	private object? PerformLookup(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		int keysConsumed = 1;
		IRefTargetContainer? target = null;
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
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Pointer;
using Json.Schema;
using Vocabularies = Json.Schema.OpenApi.Vocabularies;

namespace OpenApi.Models;

/// <summary>
/// Models the OpenAPI document.
/// </summary>
[JsonConverter(typeof(OpenApiDocumentJsonConverter))]
public class OpenApiDocument : IBaseDocument
{
	private static readonly string[] SupportedVersions =
	{
		"3.0.0",
		"3.0.1",
		"3.0.2",
		"3.0.3",
		"3.1.0"
	};
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

	/// <summary>
	/// Gets the OpenAPI document version.
	/// </summary>
	public string OpenApi { get; }
	/// <summary>
	/// Gets the API information.
	/// </summary>
	public OpenApiInfo Info { get; }
	/// <summary>
	/// Gets or sets the default JSON Schema dialect.
	/// </summary>
	public Uri? JsonSchemaDialect { get; set; }
	/// <summary>
	/// Gets or sets the server collection.
	/// </summary>
	public IEnumerable<Server>? Servers { get; set; }
	/// <summary>
	/// Gets or sets the paths collection.
	/// </summary>
	public PathCollection? Paths { get; set; }
	/// <summary>
	/// Gets or sets the webhooks collection.
	/// </summary>
	public Dictionary<string, PathItem>? Webhooks { get; set; }
	/// <summary>
	/// Gets or sets the components collection.
	/// </summary>
	public ComponentCollection? Components { get; set; }
	/// <summary>
	/// Gets or sets the security requirements collection.
	/// </summary>
	public IEnumerable<SecurityRequirement>? Security { get; set; }
	/// <summary>
	/// Gets or sets the tags.
	/// </summary>
	public IEnumerable<Tag>? Tags { get; set; }
	/// <summary>
	/// Gets or sets external documentation.
	/// </summary>
	public ExternalDocumentation? ExternalDocs { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	Uri IBaseDocument.BaseUri { get; } = GenerateBaseUri();

	private static Uri GenerateBaseUri() => new($"openapi:stj.openapi.models:{Guid.NewGuid().ToString("N")[..10]}");

	static OpenApiDocument()
	{
		Json.Schema.Formats.Register(Formats.Double);
		Json.Schema.Formats.Register(Formats.Float);
		Json.Schema.Formats.Register(Formats.Int32);
		Json.Schema.Formats.Register(Formats.Int64);
		Json.Schema.Formats.Register(Formats.Password);

		VocabularyRegistry.Global.Register(Vocabularies.OpenApi);
	}

	/// <summary>
	/// Creates a new <see cref="OpenApiDocument"/>
	/// </summary>
	/// <param name="openApi">The OpenAPI version</param>
	/// <param name="info">The API information</param>
	public OpenApiDocument(string openApi, OpenApiInfo info)
	{
		OpenApi = openApi;
		Info = info;
	}

	JsonSchema? IBaseDocument.FindSubschema(JsonPointer pointer, EvaluationOptions options)
	{
		return Find<JsonSchema>(pointer);
	}

	internal static OpenApiDocument FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var openapi = obj.ExpectString("openapi", "open api document");
		if (!SupportedVersions.Contains(openapi))
			throw new JsonException($"Version '{openapi}' is not supported.");

		var document = new OpenApiDocument(
			openapi,
			obj.Expect("info", "open api document", OpenApiInfo.FromNode))
		{
			JsonSchemaDialect = obj.MaybeUri("jsonSchemaDialect", "open api document"),
			Servers = obj.MaybeArray("servers", Server.FromNode),
			Paths = obj.Maybe("paths", PathCollection.FromNode),
			Webhooks = obj.MaybeMap("webhooks", PathItem.FromNode),
			Components = obj.Maybe("components", ComponentCollection.FromNode),
			Security = obj.MaybeArray("security", SecurityRequirement.FromNode),
			Tags = obj.MaybeArray("tags", Tag.FromNode),
			ExternalDocs = obj.Maybe("externalDocs", ExternalDocumentation.FromNode),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, document.ExtensionData?.Keys);

		return document;
	}

	internal static JsonNode? ToNode(OpenApiDocument? document, JsonSerializerOptions? options)
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

	/// <summary>
	/// Initializes the document model.
	/// </summary>
	/// <param name="schemaRegistry"></param>
	/// <exception cref="RefResolutionException">Thrown if a reference cannot be resolved.</exception>
	public async Task Initialize(SchemaRegistry? schemaRegistry = null)
	{
		schemaRegistry ??= SchemaRegistry.Global;

		schemaRegistry.Register(this);

		// find all JSON Schemas and populate their base URIs (if they don't have $id)
		RegisterSchemas(schemaRegistry);

		// find and attempt to resolve all reference objects
		await TryResolveRefs();
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

	private async Task TryResolveRefs()
	{
		var allRefs = GeneralHelpers.Collect(
			Paths?.FindRefs(),
			Webhooks?.Values.SelectMany(x => x.FindRefs()),
			Components?.FindRefs()
		);

		await Task.WhenAll(allRefs.Select(x => x.Resolve(this)));
	}

	/// <summary>
	/// Finds and retrieves an object within the document at a specified location.
	/// </summary>
	/// <typeparam name="T">The type of object</typeparam>
	/// <param name="pointer">The expected location</param>
	/// <returns>The object, if an object of that type exists at that location; otherwise null.</returns>
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

internal class OpenApiDocumentJsonConverter : JsonConverter<OpenApiDocument>
{
	public override OpenApiDocument Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return OpenApiDocument.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, OpenApiDocument value, JsonSerializerOptions options)
	{
		var json = OpenApiDocument.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}
using Json.Pointer;
using Json.Schema;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Graeae.Models;

/// <summary>
/// Models the OpenAPI document.
/// </summary>
[JsonConverter(typeof(OpenApiDocumentJsonConverter))]
public class OpenApiDocument : IBaseDocument
{
	private static readonly string[] SupportedVersions =
    [
        "3.0.0",
		"3.0.1",
		"3.0.2",
		"3.0.3",
		"3.0.4",
		"3.1.0",
		"3.1.1"
    ];
	private static readonly string[] KnownKeys =
    [
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
    ];

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
	public IReadOnlyList<Server>? Servers { get; set; }
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
	public IReadOnlyList<SecurityRequirement>? Security { get; set; }
	/// <summary>
	/// Gets or sets the tags.
	/// </summary>
	public IReadOnlyList<Tag>? Tags { get; set; }
	/// <summary>
	/// Gets or sets external documentation.
	/// </summary>
	public ExternalDocumentation? ExternalDocs { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	Uri IBaseDocument.BaseUri { get; } = GenerateBaseUri();

	private static Uri GenerateBaseUri() => new($"graeae:models:{Guid.NewGuid().ToString("N").AsSpan(0, 10).ToString()}");

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

	JsonSchemaNode? IBaseDocument.FindSubschema(JsonPointer pointer, BuildContext context)
	{
		return Find<JsonSchema>(pointer)?.Root;
	}

	public static OpenApiDocument Build(JsonElement node, BuildOptions? schemaBuildOptions = null)
	{
        schemaBuildOptions ??= BuildOptions.Default;

        if (node.ValueKind != JsonValueKind.Object)
			throw new JsonException("Expected an object");

		var openapi = node.ExpectString("openapi", "open api document");
		if (!SupportedVersions.Contains(openapi))
			throw new JsonException($"Version '{openapi}' is not supported.");

		var document = new OpenApiDocument(
			openapi,
            node.Expect("info", "open api document", OpenApiInfo.FromNode))
		{
			JsonSchemaDialect = node.MaybeUri("jsonSchemaDialect", "open api document"),
			Servers = node.MaybeArray("servers", Server.FromNode),
			Paths = node.Maybe("paths", node1 => PathCollection.FromNode(node1, schemaBuildOptions)),
			Webhooks = node.MaybeMap("webhooks", node1 => PathItem.FromNode(node1, schemaBuildOptions)),
			Components = node.Maybe("components", node1 => ComponentCollection.FromNode(node1, schemaBuildOptions)),
			Security = node.MaybeArray("security", SecurityRequirement.FromNode),
			Tags = node.MaybeArray("tags", Tag.FromNode),
			ExternalDocs = node.Maybe("externalDocs", ExternalDocumentation.FromNode),
			ExtensionData = ExtensionData.FromNode(node)
		};

        node.ValidateNoExtraKeys(KnownKeys, document.ExtensionData?.Keys);
        // find and attempt to resolve all reference objects
        document.TryResolveRefs(schemaBuildOptions);

        schemaBuildOptions.SchemaRegistry.Register(document);

        return document;
	}

    public void Initialize(BuildOptions? schemaBuildOptions = null)
    {
        schemaBuildOptions ??= BuildOptions.Default;
  
        TryResolveRefs(schemaBuildOptions);

        schemaBuildOptions.SchemaRegistry.Register(this);
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

	private void TryResolveRefs(BuildOptions buildOptions)
	{
		var allRefs = GeneralHelpers.Collect(
			Paths?.FindRefs(),
			Webhooks?.Values.SelectMany(x => x.FindRefs()),
			Components?.FindRefs()
		);

		allRefs.AsParallel().ForAll(x => x.Resolve(this, buildOptions));
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
			var keys = pointer.ToArray();

			val = PerformLookup(keys) as T;
			if (val != null)
				_lookup[pointer] = val;
		}

		if (val is JsonNode node && typeof(T) != typeof(JsonNode))
			return node.Deserialize<T>();

		return val as T;
	}

	private object? PerformLookup(ReadOnlySpan<string> keys)
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
	public override OpenApiDocument Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var obj = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
        if (obj.ValueKind is not JsonValueKind.Object)
            throw new JsonException("Expected an object");

		return OpenApiDocument.Build(obj, BuildOptions.Default);
	}

	public override void Write(Utf8JsonWriter writer, OpenApiDocument value, JsonSerializerOptions options)
	{
		var json = OpenApiDocument.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}
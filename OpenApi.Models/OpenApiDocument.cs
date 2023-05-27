using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Pointer;
using Json.Schema;

namespace OpenApi.Models;

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
			Webhooks = obj.MaybeMap("webhooks", x =>PathItem.FromNode(x, options)),
			Components = obj.Maybe("components", x=> ComponentCollection.FromNode(x, options)),
			Security = obj.MaybeArray("security", SecurityRequirement.FromNode),
			Tags = obj.MaybeArray("tags", x => Tag.FromNode(x, options)),
			ExternalDocs = obj.Maybe("externalDocs", ExternalDocumentation.FromNode),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, document.ExtensionData?.Keys);

		return document;
	}

	JsonSchema? IBaseDocument.FindSubschema(JsonPointer pointer, EvaluationOptions options)
	{
		throw new NotImplementedException();
	}
}
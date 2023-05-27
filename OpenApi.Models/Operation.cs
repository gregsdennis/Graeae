using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenApi.Models;

public class Operation
{
	private static readonly string[] KnownKeys =
	{
		"tags",
		"summary",
		"description",
		"externalDocs",
		"operationId",
		"parameters",
		"requestBody",
		"responses",
		"callbacks",
		"deprecated",
		"security",
		"servers"
	};

	public IEnumerable<string>? Tags { get; set; }
	public string? Summary { get; set; }
	public string? Description { get; set; }
	public ExternalDocumentation? ExternalDocs { get; set; }
	public string? OperationId { get; set; }
	public IEnumerable<Parameter>? Parameters { get; set; }
	public RequestBody? RequestBody { get; set; }
	public ResponseCollection? Responses { get; set; }
	public Dictionary<string, Callback>? Callbacks { get; set; }
	public bool? Deprecated { get; set; }
	public IEnumerable<SecurityRequirement>? Security { get; set; }
	public IEnumerable<Server>? Servers { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public static Operation FromNode(JsonNode? node, JsonSerializerOptions? options)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var operation = new Operation
		{
			Tags = obj.MaybeArray("tags", x => x is JsonValue v && v.TryGetValue(out string? s) ? s : throw new JsonException("tags must be strings")),
			Summary = obj.MaybeString("summary", "operation"),
			Description = obj.MaybeString("description", "operation"),
			ExternalDocs = obj.Maybe("externalDocs", ExternalDocumentation.FromNode),
			OperationId = obj.MaybeString("operationId", "operation"),
			Parameters = obj.MaybeArray("parameters", x => Parameter.FromNode(x, options)),
			RequestBody = obj.Maybe("requestBody", x => RequestBody.FromNode(x, options)),
			Responses = obj.Maybe("responses", x => ResponseCollection.FromNode(x, options)),
			Callbacks = obj.MaybeMap("callbacks", x => Callback.FromNode(x, options)),
			Deprecated = obj.MaybeBool("deprecated", "operation"),
			Security = obj.MaybeArray("security", SecurityRequirement.FromNode),
			Servers = obj.MaybeArray("servers", Server.FromNode),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, operation.ExtensionData?.Keys);

		return operation;
	}
}
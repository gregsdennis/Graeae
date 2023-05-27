using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Schema;

namespace OpenApi.Models;

[JsonConverter(typeof(ComponentCollectionJsonConverter))]
public class ComponentCollection
{
	private static readonly string[] KnownKeys =
	{
		"schemas",
		"responses",
		"parameters",
		"examples",
		"requestBodies",
		"headers",
		"securitySchemes",
		"links",
		"callbacks",
		"pathItems"
	};

	public Dictionary<string, JsonSchema>? Schemas { get; set; }
	public Dictionary<string, Response>? Responses { get; set; }
	public Dictionary<string, Parameter>? Parameters { get; set; }
	public Dictionary<string, Example>? Examples { get; set; }
	public Dictionary<string, RequestBody>? RequestBodies { get; set; }
	public Dictionary<string, Header>? Headers { get; set; }
	public Dictionary<string, SecurityScheme>? SecuritySchemas { get; set; }
	public Dictionary<string, Link>? Links { get; set; }
	public Dictionary<string, Callback>? Callbacks { get; set; }
	public Dictionary<string, PathItem>? PathItems { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public static ComponentCollection FromNode(JsonNode? node, JsonSerializerOptions? options)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var components = new ComponentCollection
		{
			Schemas = obj.MaybeDeserialize<Dictionary<string, JsonSchema>>("schemas", options),
			Responses = obj.MaybeMap("responses", x => Response.FromNode(x, options)),
			Parameters = obj.MaybeMap("parameters", x => Parameter.FromNode(x, options)),
			Examples = obj.MaybeMap("examples", x => Example.FromNode(x)),
			RequestBodies = obj.MaybeMap("requestBodies", x => RequestBody.FromNode(x, options)),
			Headers = obj.MaybeMap("headers", x => Header.FromNode(x, options)),
			SecuritySchemas = obj.MaybeMap("securitySchemes", SecurityScheme.FromNode),
			Links = obj.MaybeMap("links", x => Link.FromNode(x, options)),
			Callbacks = obj.MaybeMap("callbacks", x => Callback.FromNode(x, options)),
			PathItems = obj.MaybeMap("pathItems", x => PathItem.FromNode(x, options)),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, components.ExtensionData?.Keys);

		return components;
	}
}

public class ComponentCollectionJsonConverter : JsonConverter<ComponentCollection>
{
	public override ComponentCollection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return ComponentCollection.FromNode(obj, options);
	}

	public override void Write(Utf8JsonWriter writer, ComponentCollection value, JsonSerializerOptions options)
	{
		throw new NotImplementedException();
	}
}
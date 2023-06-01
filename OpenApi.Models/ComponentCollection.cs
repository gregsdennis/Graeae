using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace OpenApi.Models;

public class ComponentCollection : IRefResolvable
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
	public Dictionary<string, SecurityScheme>? SecuritySchemes { get; set; }
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
			Examples = obj.MaybeMap("examples", Example.FromNode),
			RequestBodies = obj.MaybeMap("requestBodies", x => RequestBody.FromNode(x, options)),
			Headers = obj.MaybeMap("headers", x => Header.FromNode(x, options)),
			SecuritySchemes = obj.MaybeMap("securitySchemes", SecurityScheme.FromNode),
			Links = obj.MaybeMap("links", x => Link.FromNode(x, options)),
			Callbacks = obj.MaybeMap("callbacks", x => Callback.FromNode(x, options)),
			PathItems = obj.MaybeMap("pathItems", x => PathItem.FromNode(x, options)),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, components.ExtensionData?.Keys);

		return components;
	}

	public static JsonNode? ToNode(ComponentCollection? components, JsonSerializerOptions? options)
	{
		if (components == null) return null;

		var obj = new JsonObject();

		obj.MaybeSerialize("schemas", components.Schemas, options);
		obj.MaybeAddMap("responses", components.Responses, x => Response.ToNode(x, options));
		obj.MaybeAddMap("parameters", components.Parameters, x => Parameter.ToNode(x, options));
		obj.MaybeAddMap("examples", components.Examples, Example.ToNode);
		obj.MaybeAddMap("requestBodies", components.RequestBodies, x => RequestBody.ToNode(x, options));
		obj.MaybeAddMap("headers", components.Headers, x => Header.ToNode(x, options));
		obj.MaybeAddMap("securitySchemes", components.SecuritySchemes, SecurityScheme.ToNode);
		obj.MaybeAddMap("links", components.Links, x => Link.ToNode(x, options));
		obj.MaybeAddMap("callbacks", components.Callbacks, x => Callback.ToNode(x, options));
		obj.MaybeAddMap("pathItems", components.PathItems, x => PathItem.ToNode(x, options));
		obj.AddExtensions(components.ExtensionData);

		return obj;
	}

	public object? Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		IRefResolvable? target = null;
		switch (keys[0])
		{
			case "schemas":
				if (Schemas == null || !Schemas.TryGetValue(keys[1], out var targetSchema)) return null;
				// TODO: consider some other kind of value being buried in a schema
				throw new NotImplementedException();
			case "responses":
				if (keys.Length == 1) return null;
				target = Responses?.GetFromMap(keys[1]);
				break;
			case "parameters":
				if (keys.Length == 1) return null;
				target = Parameters?.GetFromMap(keys[1]);
				break;
			case "examples":
				if (keys.Length == 1) return null;
				target = Examples?.GetFromMap(keys[1]);
				break;
			case "requestBodies":
				if (keys.Length == 1) return null;
				target = RequestBodies?.GetFromMap(keys[1]);
				break;
			case "headers":
				if (keys.Length == 1) return null;
				target = Headers?.GetFromMap(keys[1]);
				break;
			case "securitySchemes":
				if (keys.Length == 1) return null;
				target = SecuritySchemes?.GetFromMap(keys[1]);
				break;
			case "links":
				if (keys.Length == 1) return null;
				target = Links?.GetFromMap(keys[1]);
				break;
			case "callbacks":
				if (keys.Length == 1) return null;
				target = Callbacks?.GetFromMap(keys[1]);
				break;
			case "pathItems":
				if (keys.Length == 1) return null;
				target = PathItems?.GetFromMap(keys[1]);
				break;
		}

		return target != null
			? target.Resolve(keys[2..])
			: ExtensionData?.Resolve(keys);
	}
}

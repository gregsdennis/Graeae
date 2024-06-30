using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Schema;

namespace Graeae.Models;

/// <summary>
/// Models the `components` collection.
/// </summary>
[JsonConverter(typeof(ComponentCollectionJsonConverter))]
public class ComponentCollection : IRefTargetContainer
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

	/// <summary>
	/// Gets or sets the schema components.
	/// </summary>
	public Dictionary<string, JsonSchema>? Schemas { get; set; }
	/// <summary>
	/// Gets or sets the response components.
	/// </summary>
	public Dictionary<string, Response>? Responses { get; set; }
	/// <summary>
	/// Gets or sets the parameter components.
	/// </summary>
	public Dictionary<string, Parameter>? Parameters { get; set; }
	/// <summary>
	/// Gets or sets the example components.
	/// </summary>
	public Dictionary<string, Example>? Examples { get; set; }
	/// <summary>
	/// Gets or sets the request body components.
	/// </summary>
	public Dictionary<string, RequestBody>? RequestBodies { get; set; }
	/// <summary>
	/// Gets or sets the header components.
	/// </summary>
	public Dictionary<string, Header>? Headers { get; set; }
	/// <summary>
	/// Gets or sets the security scheme components.
	/// </summary>
	public Dictionary<string, SecurityScheme>? SecuritySchemes { get; set; }
	/// <summary>
	/// Gets or sets the link components.
	/// </summary>
	public Dictionary<string, Link>? Links { get; set; }
	/// <summary>
	/// Gets or sets the callback components.
	/// </summary>
	public Dictionary<string, Callback>? Callbacks { get; set; }
	/// <summary>
	/// Gets or sets the path item components.
	/// </summary>
	public Dictionary<string, PathItem>? PathItems { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	internal static ComponentCollection FromNode(JsonNode? node, JsonSerializerOptions? options)
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
			SecuritySchemes = obj.MaybeMap("securitySchemes", x => SecurityScheme.FromNode(x, options)),
			Links = obj.MaybeMap("links", Link.FromNode),
			Callbacks = obj.MaybeMap("callbacks", x => Callback.FromNode(x, options)),
			PathItems = obj.MaybeMap("pathItems", x => PathItem.FromNode(x, options)),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, components.ExtensionData?.Keys);

		return components;
	}

	internal static JsonNode? ToNode(ComponentCollection? components, JsonSerializerOptions? options)
	{
		if (components == null) return null;

		var obj = new JsonObject();

		obj.MaybeSerialize("schemas", components.Schemas, options);
		obj.MaybeAddMap("responses", components.Responses, x => Response.ToNode(x, options));
		obj.MaybeAddMap("parameters", components.Parameters, x => Parameter.ToNode(x, options));
		obj.MaybeAddMap("examples", components.Examples, Example.ToNode);
		obj.MaybeAddMap("requestBodies", components.RequestBodies, x => RequestBody.ToNode(x, options));
		obj.MaybeAddMap("headers", components.Headers, x => Header.ToNode(x, options));
		obj.MaybeAddMap("securitySchemes", components.SecuritySchemes, x => SecurityScheme.ToNode(x, options));
		obj.MaybeAddMap("links", components.Links, Link.ToNode);
		obj.MaybeAddMap("callbacks", components.Callbacks, x => Callback.ToNode(x, options));
		obj.MaybeAddMap("pathItems", components.PathItems, x => PathItem.ToNode(x, options));
		obj.AddExtensions(components.ExtensionData);

		return obj;
	}

	object? IRefTargetContainer.Resolve(ReadOnlySpan<string> keys)
	{
		if (keys.Length == 0) return this;

		IRefTargetContainer? target = null;
		switch (keys[0])
		{
			case "schemas":
				if (Schemas == null || !Schemas.TryGetValue(keys[1], out var targetSchema)) return null;
				if (keys.Length == 2) return targetSchema;
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

	internal IEnumerable<JsonSchema> FindSchemas()
	{
		return GeneralHelpers.Collect(Schemas?.Values,
			Responses?.Values.SelectMany(x => x.FindSchemas()),
			Parameters?.Values.SelectMany(x => x.FindSchemas()),
			RequestBodies?.Values.SelectMany(x => x.FindSchemas()),
			Headers?.Values.SelectMany(x => x.FindSchemas()),
			Callbacks?.Values.SelectMany(x => x.FindSchemas()),
			PathItems?.Values.SelectMany(x => x.FindSchemas())
		);
	}

	internal IEnumerable<IComponentRef> FindRefs()
	{
		return GeneralHelpers.Collect(
			Responses?.Values.SelectMany(x => x.FindRefs()),
			Parameters?.Values.SelectMany(x => x.FindRefs()),
			Examples?.Values.SelectMany(x => x.FindRefs()),
			RequestBodies?.Values.SelectMany(x => x.FindRefs()),
			Headers?.Values.SelectMany(x => x.FindRefs()),
			SecuritySchemes?.Values.SelectMany(x => x.FindRefs()),
			Links?.Values.SelectMany(x => x.FindRefs()),
			Callbacks?.Values.SelectMany(x => x.FindRefs()),
			PathItems?.Values.SelectMany(x => x.FindRefs())
		);
	}
}

internal class ComponentCollectionJsonConverter : JsonConverter<ComponentCollection>
{
	public override ComponentCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return ComponentCollection.FromNode(obj, options);
	}

	public override void Write(Utf8JsonWriter writer, ComponentCollection value, JsonSerializerOptions options)
	{
		var json = ComponentCollection.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.More;
using Json.Schema;

namespace OpenApi.Models;

[JsonConverter(typeof(HeaderJsonConverter))]
public class Header : IRefTargetContainer
{
	private static readonly string[] KnownKeys =
	{
		"description",
		"required",
		"deprecated",
		"allowEmptyValue",
		"style",
		"explode",
		"allowReserved",
		"schema",
		"example",
		"examples",
		"content"
	};

	public string? Description { get; set; }
	public bool? Required { get; set; }
	public bool? Deprecated { get; set; }
	public bool? AllowEmptyValue { get; set; }
	public ParameterStyle? Style { get; set; }
	public bool? Explode { get; set; }
	public bool? AllowReserved { get; set; }
	public JsonSchema? Schema { get; set; }
	public JsonNode? Example { get; set; } // use JsonNull
	public Dictionary<string, Example>? Examples { get; set; }
	public Dictionary<string, MediaType>? Content { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public static Header FromNode(JsonNode? node, JsonSerializerOptions? options)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		if (obj.ContainsKey("$ref"))
		{
			var response = new HeaderRef(obj.ExpectUri("$ref", "reference"))
			{
				Description = obj.MaybeString("description", "reference"),
				Summary = obj.MaybeString("summary", "reference")
			};

			obj.ValidateReferenceKeys();

			return response;
		}
		else
		{
			var response = new Header
			{
				Description = obj.MaybeString("description", "header"),
				Required = obj.MaybeBool("required", "header"),
				Deprecated = obj.MaybeBool("deprecated", "header"),
				AllowEmptyValue = obj.MaybeBool("allowEmptyValue", "header"),
				Style = obj.MaybeEnum<ParameterStyle>("style", "header"),
				Explode = obj.MaybeBool("explode", "header"),
				AllowReserved = obj.MaybeBool("allowReserved", "header"),
				Schema = obj.MaybeDeserialize<JsonSchema>("schema", options),
				Example = obj.TryGetPropertyValue("example", out var v) ? v ?? JsonNull.SignalNode : null,
				Examples = obj.MaybeMap("examples", Models.Example.FromNode),
				Content = obj.MaybeMap("content", x => MediaType.FromNode(x, options)),
				ExtensionData = ExtensionData.FromNode(obj)
			};

			obj.ValidateNoExtraKeys(KnownKeys, response.ExtensionData?.Keys);

			return response;
		}
	}

	public static JsonNode? ToNode(Header? header, JsonSerializerOptions? options)
	{
		if (header == null) return null;

		var obj = new JsonObject();

		if (header is HeaderRef reference)
		{
			obj.Add("$ref", reference.Ref.ToString());
			obj.MaybeAdd("description", reference.Description);
			obj.MaybeAdd("summary", reference.Summary);
		}
		else
		{
			obj.MaybeAdd("description", header.Description);
			obj.MaybeAdd("required", header.Required);
			obj.MaybeAdd("deprecated", header.Deprecated);
			obj.MaybeAdd("allowEmptyValues", header.AllowEmptyValue);
			obj.MaybeAddEnum("style", header.Style);
			obj.MaybeAdd("explode", header.Explode);
			obj.MaybeAdd("allowReserved", header.AllowReserved);
			obj.MaybeSerialize("schema", header.Schema, options);
			obj.MaybeAdd("example", header.Example.Copy());
			obj.MaybeAddMap("examples", header.Examples, Models.Example.ToNode);
			obj.MaybeAddMap("content", header.Content, x => MediaType.ToNode(x, options));
			obj.AddExtensions(header.ExtensionData);
		}

		return obj;
	}

	public object? Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		int keysConsumed = 1;
		IRefTargetContainer? target = null;
		switch (keys[0])
		{
			case "schema":
				if (Schema == null) return null;
				if (keys.Length == 1) return Schema;
				// TODO: consider some other kind of value being buried in a schema
				throw new NotImplementedException();
			case "example":
				return Example?.GetFromNode(keys[1..]);
			case "examples":
				if (keys.Length == 1) return null;
				keysConsumed++;
				target = Examples?.GetFromMap(keys[1]);
				break;
			case "content":
				if (keys.Length == 1) return null;
				keysConsumed++;
				target = Content?.GetFromMap(keys[1]);
				break;
		}

		return target != null
			? target.Resolve(keys[keysConsumed..])
			: ExtensionData?.Resolve(keys);
	}

	public IEnumerable<JsonSchema> FindSchemas()
	{
		if (Schema != null)
			yield return Schema;

		var theRest = GeneralHelpers.Collect(Content?.Values.SelectMany(x => x.FindSchemas()));

		foreach (var schema in theRest)
		{
			yield return schema;
		}
	}

	public IEnumerable<IComponentRef> FindRefs()
	{
		if (this is HeaderRef hRef)
			yield return hRef;

		var theRest = GeneralHelpers.Collect(
			Examples?.Values.SelectMany(x => x.FindRefs()),
			Content?.Values.SelectMany(x => x.FindRefs())
		);

		foreach (var reference in theRest)
		{
			yield return reference;
		}
	}
}

public class HeaderRef : Header, IComponentRef
{
	public Uri Ref { get;  }
	public string? Summary { get; set; }
	public new string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public HeaderRef(Uri reference)
	{
		Ref = reference ?? throw new ArgumentNullException(nameof(reference));
	}

	public void Resolve(OpenApiDocument root)
	{
		// resolve the $ref and set all of the props
		// remember to use base.Description

		IsResolved = true;
	}
}

public class HeaderJsonConverter : JsonConverter<Header>
{
	public override Header? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return Header.FromNode(obj, options);
	}

	public override void Write(Utf8JsonWriter writer, Header value, JsonSerializerOptions options)
	{
		var json = Header.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}

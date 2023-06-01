using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenApi.Models;

[JsonConverter(typeof(EncodingJsonConverter))]
public class Encoding : IRefResolvable
{
	private static readonly string[] KnownKeys =
	{
		"contentType",
		"headers",
		"style",
		"explode",
		"allowReserved"
	};

	public string? ContentType { get; set; }
	public Dictionary<string, Header>? Headers { get; set; }
	public ParameterStyle? Style { get; set; }
	public bool? Explode { get; set; }
	public bool? AllowReserved { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public static Encoding FromNode(JsonNode? node, JsonSerializerOptions? options)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var encoding = new Encoding
		{
			ContentType = obj.MaybeString("contentType", "encoding"),
			Headers = obj.MaybeMap("headers", x => Header.FromNode(x, options)),
			Style = obj.MaybeEnum<ParameterStyle>("style", "encoding"),
			Explode = obj.MaybeBool("explode", "encoding"),
			AllowReserved = obj.MaybeBool("allowReserved", "encoding"),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, encoding.ExtensionData?.Keys);

		return encoding;
	}

	public static JsonNode? ToNode(Encoding? encoding, JsonSerializerOptions? options)
	{
		if (encoding == null) return null;

		var obj = new JsonObject();

		obj.MaybeAdd("contentType", encoding.ContentType);
		obj.MaybeAddMap("headers", encoding.Headers, x => Header.ToNode(x, options));
		obj.MaybeAddEnum("style", encoding.Style);
		obj.MaybeAdd("explode", encoding.Explode);
		obj.MaybeAdd("allowReserved", encoding.AllowReserved);
		obj.AddExtensions(encoding.ExtensionData);

		return obj;
	}

	public object? Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		if (keys[0] == "headers")
		{
			if (keys.Length == 1) return null;
			return Headers.GetFromMap(keys[1])?.Resolve(keys[2..]);
		}

		return ExtensionData?.Resolve(keys);
	}
}

public class EncodingJsonConverter : JsonConverter<Encoding>
{
	public override Encoding? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return Encoding.FromNode(obj, options);
	}

	public override void Write(Utf8JsonWriter writer, Encoding value, JsonSerializerOptions options)
	{
		var json = Encoding.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}

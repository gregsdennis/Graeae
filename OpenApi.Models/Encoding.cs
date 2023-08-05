using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;

namespace OpenApi.Models;

/// <summary>
/// Models an encoding object.
/// </summary>
[JsonConverter(typeof(EncodingJsonConverter))]
public class Encoding : IRefTargetContainer
{
	private static readonly string[] KnownKeys =
	{
		"contentType",
		"headers",
		"style",
		"explode",
		"allowReserved"
	};

	/// <summary>
	/// Gets or sets the encoding content type.
	/// </summary>
	public string? ContentType { get; set; }
	/// <summary>
	/// Gets or sets headers.
	/// </summary>
	public Dictionary<string, Header>? Headers { get; set; }
	/// <summary>
	/// Gets or sets the encoding parameter style.
	/// </summary>
	public ParameterStyle? Style { get; set; }
	/// <summary>
	/// Gets or sets whether this will be exploded into multiple parameters.
	/// </summary>
	public bool? Explode { get; set; }
	/// <summary>
	/// Gets or sets whether the parameter value SHOULD allow reserved characters.
	/// </summary>
	public bool? AllowReserved { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	internal static Encoding FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var encoding = new Encoding
		{
			ContentType = obj.MaybeString("contentType", "encoding"),
			Headers = obj.MaybeMap("headers", Header.FromNode),
			Style = obj.MaybeEnum<ParameterStyle>("style", "encoding"),
			Explode = obj.MaybeBool("explode", "encoding"),
			AllowReserved = obj.MaybeBool("allowReserved", "encoding"),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, encoding.ExtensionData?.Keys);

		return encoding;
	}

	internal static JsonNode? ToNode(Encoding? encoding, JsonSerializerOptions? options)
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

	object? IRefTargetContainer.Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		if (keys[0] == "headers")
		{
			if (keys.Length == 1) return null;
			return Headers.GetFromMap(keys[1])?.Resolve(keys[2..]);
		}

		return ExtensionData?.Resolve(keys);
	}

	internal IEnumerable<JsonSchema> FindSchemas()
	{
		return Headers?.Values.SelectMany(x => x.FindSchemas()) ?? Enumerable.Empty<JsonSchema>();
	}

	internal IEnumerable<IComponentRef> FindRefs()
	{
		return GeneralHelpers.Collect(
			Headers?.Values.SelectMany(x => x.FindRefs())
		);
	}
}

internal class EncodingJsonConverter : JsonConverter<Encoding>
{
	public override Encoding Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return Encoding.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, Encoding value, JsonSerializerOptions options)
	{
		var json = Encoding.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}

using System.Text.Json.Nodes;
using System.Text.Json;

namespace OpenApi.Models;

public class Encoding
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
}
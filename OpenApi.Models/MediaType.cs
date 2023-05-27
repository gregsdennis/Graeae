using System.Text.Json;
using System.Text.Json.Nodes;
using Json.More;
using Json.Schema;

namespace OpenApi.Models;

public class MediaType
{
	private static readonly string[] KnownKeys =
	{
		"schema",
		"example",
		"examples",
		"encoding"
	};

	public JsonSchema? Schema { get; set; }
	public JsonNode? Example { get; set; } // use JsonNull
	public Dictionary<string, Example>? Examples { get; set; }
	public Dictionary<string, Encoding>? Encoding { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public static MediaType FromNode(JsonNode? node, JsonSerializerOptions? options)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var mediaType = new MediaType
		{
			Schema = obj.MaybeDeserialize<JsonSchema>("schema", options),
			Example = obj.TryGetPropertyValue("example", out var v) ? v ?? JsonNull.SignalNode : null,
			Examples = obj.MaybeMap("examples", Models.Example.FromNode),
			Encoding = obj.MaybeMap("encoding", x => Models.Encoding.FromNode(x, options)),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, mediaType.ExtensionData?.Keys);

		return mediaType;
	}
}
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.More;
using Json.Schema;

namespace OpenApi.Models;

public class MediaType : IRefResolvable
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

	public static JsonNode? ToNode(MediaType? mediaType, JsonSerializerOptions? options)
	{
		if (mediaType == null) return null;

		var obj = new JsonObject();

		obj.MaybeSerialize("schema", mediaType.Schema, options);
		obj.MaybeAdd("example", mediaType.Example.Copy());
		obj.MaybeAddMap("examples", mediaType.Examples, Models.Example.ToNode);
		obj.MaybeAddMap("encoding", mediaType.Encoding, x => Models.Encoding.ToNode(x, options));
		obj.AddExtensions(mediaType.ExtensionData);

		return obj;
	}

	public object? Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		int keysConsumed = 1;
		IRefResolvable? target = null;
		switch (keys[0])
		{
			case "schema":
				if (Schema == null) return null;
				// TODO: consider some other kind of value being buried in a schema
				throw new NotImplementedException();
			case "example":
				return Example?.GetFromNode(keys[1..]);
			case "examples":
				if (keys.Length == 1) return null;
				keysConsumed++;
				target = Examples?.GetFromMap(keys[1]);
				break;
			case "encoding":
				if (keys.Length == 1) return null;
				keysConsumed++;
				target = Encoding?.GetFromMap(keys[1]);
				break;
		}

		return target != null
			? target.Resolve(keys[keysConsumed..])
			: ExtensionData?.Resolve(keys);
	}
}
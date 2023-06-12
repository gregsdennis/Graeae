using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Schema;

namespace OpenApi.Models;

[JsonConverter(typeof(PathCollectionJsonConverter))]
public class PathCollection : Dictionary<PathTemplate, PathItem>, IRefTargetContainer
{
	public ExtensionData? ExtensionData { get; set; }

	public static PathCollection FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var collection = new PathCollection
		{
			ExtensionData = ExtensionData.FromNode(obj)
		};

		foreach (var (key, value) in obj)
		{
			if (key.StartsWith("x-")) continue;
			if (!PathTemplate.TryParse(key, out var template))
				throw new JsonException($"`{key}` is not a valid path template");

			collection.Add(template, PathItem.FromNode(value));
		}

		// Validating extra keys is done in the loop.

		return collection;
	}

	public static JsonNode? ToNode(PathCollection? paths, JsonSerializerOptions? options)
	{
		if (paths == null) return null;

		var obj = new JsonObject();

		foreach (var (key, value) in paths)
		{
			obj.Add(key.ToString(), PathItem.ToNode(value, options));
		}

		obj.AddExtensions(paths.ExtensionData);

		return obj;
	}

	public object? Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return null;

		return this.GetFromMap(keys[0])?.Resolve(keys[1..]) ??
		       ExtensionData?.Resolve(keys);
	}

	public IEnumerable<JsonSchema> FindSchemas()
	{
		return Values.SelectMany(x => x.FindSchemas());
	}

	public IEnumerable<IComponentRef> FindRefs()
	{
		return Values.SelectMany(x => x.FindRefs());
	}
}

public class PathCollectionJsonConverter : JsonConverter<PathCollection>
{
	public override PathCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return PathCollection.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, PathCollection value, JsonSerializerOptions options)
	{
		var json = PathCollection.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}

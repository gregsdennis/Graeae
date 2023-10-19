using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Schema;

namespace Graeae.Models;

/// <summary>
/// Models a path collection.
/// </summary>
[JsonConverter(typeof(PathCollectionJsonConverter))]
public class PathCollection : Dictionary<PathTemplate, PathItem>, IRefTargetContainer
{
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	internal static PathCollection FromNode(JsonNode? node, JsonSerializerOptions? options)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var collection = new PathCollection
		{
			ExtensionData = ExtensionData.FromNode(obj)
		};

		foreach (var kvp in obj)
		{
			if (kvp.Key.StartsWith("x-")) continue;
			if (!PathTemplate.TryParse(kvp.Key, out var template))
				throw new JsonException($"`{kvp.Key}` is not a valid path template");

			collection.Add(template, PathItem.FromNode(value, options));
		}

		// Validating extra keys is done in the loop.

		return collection;
	}

	internal static JsonNode? ToNode(PathCollection? paths, JsonSerializerOptions? options)
	{
		if (paths == null) return null;

		var obj = new JsonObject();

		foreach (var kvp in paths)
		{
			obj.Add(kvp.Key.ToString(), PathItem.ToNode(kvp.Value, options));
		}

		obj.AddExtensions(paths.ExtensionData);

		return obj;
	}

	object? IRefTargetContainer.Resolve(ReadOnlySpan<string> keys)
	{
		if (keys.Length == 0) return null;

		return this.GetFromMap(keys[0])?.Resolve(keys.Slice(1)) ??
		       ExtensionData?.Resolve(keys);
	}

	internal IEnumerable<JsonSchema> FindSchemas()
	{
		return Values.SelectMany(x => x.FindSchemas());
	}

	internal IEnumerable<IComponentRef> FindRefs()
	{
		return Values.SelectMany(x => x.FindRefs());
	}
}

internal class PathCollectionJsonConverter : JsonConverter<PathCollection>
{
	public override PathCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return PathCollection.FromNode(obj, options);
	}

	public override void Write(Utf8JsonWriter writer, PathCollection value, JsonSerializerOptions options)
	{
		var json = PathCollection.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}

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

	internal static PathCollection FromNode(JsonElement node, BuildOptions buildOptions)
	{
		if (node.ValueKind is not JsonValueKind.Object)
			throw new JsonException("Expected an object");

		var collection = new PathCollection
		{
			ExtensionData = ExtensionData.FromNode(node)
		};

		foreach (var kvp in node.EnumerateObject())
		{
			if (kvp.Name.StartsWith("x-")) continue;
			if (!PathTemplate.TryParse(kvp.Name, out var template))
				throw new JsonException($"`{kvp.Name}` is not a valid path template");

			collection.Add(template, PathItem.FromNode(kvp.Value, buildOptions));
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
        var obj = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
        if (obj.ValueKind is not JsonValueKind.Object)
            throw new JsonException("Expected an object");

		return PathCollection.FromNode(obj, BuildOptions.Default);
	}

	public override void Write(Utf8JsonWriter writer, PathCollection value, JsonSerializerOptions options)
	{
		var json = PathCollection.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}

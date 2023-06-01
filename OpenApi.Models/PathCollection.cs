using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenApi.Models;

public class PathCollection : Dictionary<PathTemplate, PathItem>, IRefResolvable
{
	public ExtensionData? ExtensionData { get; set; }

	public static PathCollection FromNode(JsonNode? node, JsonSerializerOptions? options)
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

			collection.Add(template, PathItem.FromNode(value, options));
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
}
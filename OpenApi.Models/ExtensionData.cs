using System.Text.Json.Nodes;

namespace OpenApi.Models;

public class ExtensionData : Dictionary<string, JsonNode?>, IRefResolvable
{
	public static ExtensionData? FromNode(JsonObject obj)
	{
		var data = new ExtensionData();
		foreach (var (key, value) in obj.Where(x => x.Key.StartsWith("x-")))
		{
			data.Add(key, value);
		}

		return data.Any() ? data : null;
	}

	public object? Resolve(Span<string> keys)
	{
		if (keys.Length == 0)
			throw new InvalidOperationException("Greg forgot to check for an empty span.");

		if (!TryGetValue(keys[0], out var jn)) return null;
		if (keys.Length == 1) return jn;

		keys[1..].ToPointer().TryEvaluate(jn, out var result);

		return result;
	}
}

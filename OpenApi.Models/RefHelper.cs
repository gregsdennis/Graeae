using System.Text.Json.Nodes;
using Json.Pointer;

namespace OpenApi.Models;

public static class RefHelper
{
	public static T? GetFromArray<T>(this IEnumerable<T>? array, string key)
		where T : class
	{
		if (!int.TryParse(key, out var index)) return null;
		return index < 1
			? array?.Reverse().ElementAtOrDefault(-index)
			: array?.ElementAtOrDefault(index);
	}
	
	public static TValue? GetFromMap<TKey, TValue>(this Dictionary<TKey, TValue>? map, string key)
		where TKey : IEquatable<string>
		where TValue : class
	{
		return map?.FirstOrDefault(x => x.Key.Equals(key)).Value;
	}

	public static object? GetFromNode(this JsonNode? node, Span<string> keys)
	{
		return keys.ToPointer().TryEvaluate(node, out var target)
			? target
			: null;
	}

	public static JsonPointer ToPointer(this Span<string> segments)
	{
		return JsonPointer.Create(segments.ToArray().Select(x => (PointerSegment)x));
	}
}
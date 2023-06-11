using System.Text.Json.Nodes;
using Json.Pointer;
using Json.Schema;

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

	public static bool Resolve<T>(OpenApiDocument root, Uri targetUri, Func<JsonNode?, bool> import, Action<T> copy)
		where T : class
	{
		var baseUri = ((IBaseDocument)root).BaseUri;
		var newUri = new Uri(baseUri, targetUri);
		var fragment = newUri.Fragment;

		var newBaseUri = new Uri(newUri.GetLeftPart(UriPartial.Query));

		if (newBaseUri == baseUri)
		{
			var target = root.Find<T>(JsonPointer.Parse(fragment));
			if (target == null) return false;

			copy(target);
			return true;
		}

		JsonNode? targetContent;
		var targetBase = Fetch(newBaseUri) ??
		                 throw new RefResolutionException($"Cannot resolve base schema from `{newUri}`");

		if (JsonPointer.TryParse(fragment, out var pointerFragment))
			pointerFragment!.TryEvaluate(targetBase, out targetContent);
		else
		{
			throw new RefResolutionException("Anchor fragments are currently unsupported.");

			//var anchorFragment = fragment.Substring(1);
			//if (!AnchorKeyword.AnchorPattern.IsMatch(anchorFragment))
			//	throw new RefResolutionException($"Unrecognized fragment type `{newUri}`");

			//if (targetBase is JsonSchema targetBaseSchema &&
			//    targetBaseSchema.Anchors.TryGetValue(anchorFragment, out var anchorDefinition))
			//	targetContent = anchorDefinition.Schema;
		}

		return import(targetContent);
	}

	// TODO: Initialize this
	public static Func<Uri, JsonNode?> Fetch { get; set; }
}
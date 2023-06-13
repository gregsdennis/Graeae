using System.Text.Json.Nodes;
using Json.Pointer;
using Json.Schema;
using Yaml2JsonNode;

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

	public static async Task<bool> Resolve<T>(OpenApiDocument root, Uri targetUri, Func<JsonNode?, bool> import, Action<T> copy)
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

		if (Fetch == null)
			throw new RefResolutionException("Automatic fetching of referenced documents has been disabled.");

		var targetBase = await Fetch(newBaseUri) ??
		                 throw new RefResolutionException($"Cannot resolve base schema from `{newUri}`");

		if (!JsonPointer.TryParse(fragment, out var pointerFragment))
			throw new RefResolutionException("URI fragments for $ref must be JSON Pointers.");
			
		pointerFragment!.TryEvaluate(targetBase, out var targetContent);

		return import(targetContent);
	}

	public static Func<Uri, Task<JsonNode?>>? Fetch { get; set; } = FetchJson;

	// This is inefficient, but it gets the job done.
	public static async Task<JsonNode?> FetchJson(Uri uri)
	{
		using var client = new HttpClient();
		var content = await client.GetStringAsync(uri);
		var yaml = YamlSerializer.Parse(content);
		var json = yaml.First().ToJsonNode();

		return json;
	}
}
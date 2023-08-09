using System.Text.Json.Nodes;
using Json.Pointer;
using Json.Schema;
using Yaml2JsonNode;

namespace Graeae.Models;

/// <summary>
/// Allows customization of `$ref` resolutions.
/// </summary>
public static class Ref
{
	/// <summary>
	/// Provides factory methods for creating references to objects in the components collection.
	/// </summary>
	public static class To
	{
		/// <summary>
		/// Creates a reference to a callback.
		/// </summary>
		/// <param name="componentName">The key that identifies the callback.</param>
		/// <returns>The reference.</returns>
		public static CallbackRef Callback(string componentName) => new($"#/components/callbacks/{componentName}");

		/// <summary>
		/// Creates a reference to a example.
		/// </summary>
		/// <param name="componentName">The key that identifies the example.</param>
		/// <returns>The reference.</returns>
		public static ExampleRef Example(string componentName) => new($"#/components/examples/{componentName}");

		/// <summary>
		/// Creates a reference to a header.
		/// </summary>
		/// <param name="componentName">The key that identifies the header.</param>
		/// <returns>The reference.</returns>
		public static HeaderRef Header(string componentName) => new($"#/components/headers/{componentName}");

		/// <summary>
		/// Creates a reference to a link.
		/// </summary>
		/// <param name="componentName">The key that identifies the link.</param>
		/// <returns>The reference.</returns>
		public static LinkRef Link(string componentName) => new($"#/components/links/{componentName}");

		/// <summary>
		/// Creates a reference to a parameter.
		/// </summary>
		/// <param name="componentName">The key that identifies the parameter.</param>
		/// <returns>The reference.</returns>
		public static ParameterRef Parameter(string componentName) => new($"#/components/parameters/{componentName}");

		/// <summary>
		/// Creates a reference to a path item.
		/// </summary>
		/// <param name="componentName">The key that identifies the path item.</param>
		/// <returns>The reference.</returns>
		public static PathItemRef PathItem(string componentName) => new($"#/components/pathItems/{componentName}");

		/// <summary>
		/// Creates a reference to a request body.
		/// </summary>
		/// <param name="componentName">The key that identifies the request body.</param>
		/// <returns>The reference.</returns>
		public static RequestBodyRef RequestBody(string componentName) => new($"#/components/requestBodies/{componentName}");

		/// <summary>
		/// Creates a reference to a response.
		/// </summary>
		/// <param name="componentName">The key that identifies the response.</param>
		/// <returns>The reference.</returns>
		public static ResponseRef Response(string componentName) => new($"#/components/responses/{componentName}");

		/// <summary>
		/// Creates a reference to a schema.
		/// </summary>
		/// <param name="componentName">The key that identifies the schema.</param>
		/// <returns>The reference.</returns>
		public static JsonSchema Schema(string componentName) => new JsonSchemaBuilder().Ref($"#/components/schemas/{componentName}");

		/// <summary>
		/// Creates a reference to a security scheme.
		/// </summary>
		/// <param name="componentName">The key that identifies the security scheme.</param>
		/// <returns>The reference.</returns>
		public static SecuritySchemeRef SecurityScheme(string componentName) => new($"#/components/securitySchemes/{componentName}");
	}

	/// <summary>
	/// Gets or sets the `$ref` fetching function.
	/// </summary>
	public static Func<Uri, Task<JsonNode?>>? Fetch { get; set; } = FetchJson;

	/// <summary>
	/// Defines a default basic fetching function that uses an
	/// <see cref="HttpClient"/> and supports YAML and JSON content.
	/// </summary>
	/// <param name="uri">The resource URI</param>
	/// <returns>The JSON content as a `JsonNode`</returns>
	public static async Task<JsonNode?> FetchJson(Uri uri)
	{
		// This is inefficient, but it gets the job done.
		using var client = new HttpClient();
		var content = await client.GetStringAsync(uri);
		var yaml = YamlSerializer.Parse(content);
		var json = yaml.First().ToJsonNode();

		return json;
	}

	internal static T? GetFromArray<T>(this IEnumerable<T>? array, string key)
		where T : class
	{
		if (!int.TryParse(key, out var index)) return null;
		return index < 1
			? array?.Reverse().ElementAtOrDefault(-index)
			: array?.ElementAtOrDefault(index);
	}

	internal static TValue? GetFromMap<TKey, TValue>(this Dictionary<TKey, TValue>? map, string key)
		where TKey : IEquatable<string>
		where TValue : class
	{
		return map?.FirstOrDefault(x => x.Key.Equals(key)).Value;
	}

	internal static object? GetFromNode(this JsonNode? node, Span<string> keys)
	{
		return keys.ToPointer().TryEvaluate(node, out var target)
			? target
			: null;
	}

	internal static JsonPointer ToPointer(this Span<string> segments)
	{
		return JsonPointer.Create(segments.ToArray().Select(x => (PointerSegment)x));
	}

	internal static async Task<bool> Resolve<T>(OpenApiDocument root, Uri targetUri, Func<JsonNode?, bool> import, Action<T> copy)
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
}
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenApi.Models;

public static class SerializationExtensions
{
	public static T? MaybeDeserialize<T>(this JsonObject obj, string propertyName, JsonSerializerOptions? options)
		where T : class
	{
		if (!obj.TryGetPropertyValue(propertyName, out var value)) return null;
		return value.Deserialize<T>(options);
	}

	public static T Expect<T>(this JsonObject obj, string propertyName, string objectType, Func<JsonNode?, T> factory)
		where T : class
	{
		if (!obj.TryGetPropertyValue(propertyName, out var value))
			throw new JsonException($"`{propertyName}` is required for {objectType} object");
		return factory(value);
	}

	public static T? Maybe<T>(this JsonObject obj, string propertyName, Func<JsonNode?, T> factory)
		where T : class
	{
		if (!obj.TryGetPropertyValue(propertyName, out var value)) return null;
		return factory(value);
	}

	public static IEnumerable<T>? MaybeArray<T>(this JsonObject obj, string propertyName, Func<JsonNode?, T> factory)
	{
		if (!obj.TryGetPropertyValue(propertyName, out var array)) return null;
		if (array is not JsonArray map)
			throw new JsonException($"Property `{propertyName}` must be an object");

		var deserialized = new List<T>();

		foreach (var value in map)
		{
			var item = factory(value);
			deserialized.Add(item);
		}

		return deserialized;
	}

	public static Dictionary<string, T> ExpectMap<T>(this JsonObject obj, string propertyName, string objectType, Func<JsonNode?, T> factory)
	{
		if (!obj.TryGetPropertyValue(propertyName, out var dict))
			throw new JsonException($"`{propertyName}` is required for {objectType} object");
		if (dict is not JsonObject map)
			throw new JsonException($"Property `{propertyName}` must be an object");

		var deserialized = new Dictionary<string, T>();

		foreach (var (key, value) in map)
		{
			var item = factory(value);
			deserialized.Add(key, item);
		}

		return deserialized;
	}

	public static Dictionary<string, T>? MaybeMap<T>(this JsonObject obj, string propertyName, Func<JsonNode?, T> factory)
	{
		if (!obj.TryGetPropertyValue(propertyName, out var dict)) return null;
		if (dict is not JsonObject map)
			throw new JsonException($"Property `{propertyName}` must be an object");

		var deserialized = new Dictionary<string, T>();

		foreach (var (key, value) in map)
		{
			var item = factory(value);
			deserialized.Add(key, item);
		}

		return deserialized;
	}

	public static string ExpectString(this JsonObject obj, string propertyName, string objectType)
	{
		if (!obj.TryGetPropertyValue(propertyName, out var n))
			throw new JsonException($"`{propertyName}` is required for {objectType} object");
		if (n is not JsonValue v || !v.TryGetValue<string>(out var s))
			throw new JsonException($"`{propertyName}` in {objectType} object must be a string");

		return s;
	}

	public static string? MaybeString(this JsonObject obj, string propertyName, string objectType)
	{
		if (!obj.TryGetPropertyValue(propertyName, out var n)) return null;			
		if (n is not JsonValue v || !v.TryGetValue<string>(out var s))
			throw new JsonException($"`{propertyName}` in {objectType} object must be a string");

		return s;
	}

	public static Uri ExpectUri(this JsonObject obj, string propertyName, string objectType)
	{
		if (!obj.TryGetPropertyValue(propertyName, out var n))
			throw new JsonException($"`{propertyName}` is required for {objectType} object");
		if (n is not JsonValue v || !v.TryGetValue<string>(out var s) || !Uri.IsWellFormedUriString(s, UriKind.RelativeOrAbsolute))
			throw new JsonException($"`{propertyName}` in {objectType} object must be a string containing a valid URI");

		return new Uri(s);
	}

	public static Uri? MaybeUri(this JsonObject obj, string propertyName, string objectType)
	{
		if (!obj.TryGetPropertyValue(propertyName, out var n)) return null;
		if (n is not JsonValue v || !v.TryGetValue<string>(out var s) || !Uri.IsWellFormedUriString(s, UriKind.RelativeOrAbsolute))
			throw new JsonException($"`{propertyName}` in {objectType} object must be a string containing a valid URI");

		return new Uri(s);
	}

	public static bool ExpectBool(this JsonObject obj, string propertyName, string objectType)
	{
		if (!obj.TryGetPropertyValue(propertyName, out var n))
			throw new JsonException($"`{propertyName}` is required for {objectType} object");
		if (n is not JsonValue v || !v.TryGetValue<bool>(out var b))
			throw new JsonException($"`{propertyName}` in {objectType} object must be a boolean");

		return b;
	}

	public static bool? MaybeBool(this JsonObject obj, string propertyName, string objectType)
	{
		if (!obj.TryGetPropertyValue(propertyName, out var n)) return null;			
		if (n is not JsonValue v || !v.TryGetValue<bool>(out var b))
			throw new JsonException($"`{propertyName}` in {objectType} object must be a boolean");

		return b;
	}

	public static T ExpectEnum<T>(this JsonObject obj, string propertyName, string objectType)
		where T : struct, Enum
	{
		if (!obj.TryGetPropertyValue(propertyName, out var n))
			throw new JsonException($"`{propertyName}` is required for {objectType} object");
		if (n is not JsonValue v || !v.TryGetValue<string>(out var s) || !Enum.TryParse(s, out T e))
			throw new JsonException($"`{propertyName}` in {objectType} object must be one of the predefined string values");

		return e;
	}

	public static T? MaybeEnum<T>(this JsonObject obj, string propertyName, string objectType)
		where T : struct, Enum
	{
		if (!obj.TryGetPropertyValue(propertyName, out var n)) return null;			
		if (n is not JsonValue v || !v.TryGetValue<string>(out var s) || !Enum.TryParse(s, out T e))
			throw new JsonException($"`{propertyName}` in {objectType} object must be one of the predefined string values");

		return e;
	}
}
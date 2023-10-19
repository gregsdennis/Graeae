using System.Text.Json;
using System.Text.Json.Nodes;
using Json.More;

namespace Graeae.Models;

internal static class SerializationExtensions
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

	public static IReadOnlyList<T>? MaybeArray<T>(this JsonObject obj, string propertyName, Func<JsonNode?, T> factory)
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

		foreach (var kvp in map)
		{
			var item = factory(kvp.Value);
			deserialized.Add(kvp.Key, item);
		}

		return deserialized;
	}

	public static Dictionary<string, T>? MaybeMap<T>(this JsonObject obj, string propertyName, Func<JsonNode?, T> factory)
	{
		if (!obj.TryGetPropertyValue(propertyName, out var dict)) return null;
		if (dict is not JsonObject map)
			throw new JsonException($"Property `{propertyName}` must be an object");

		var deserialized = new Dictionary<string, T>();

		foreach (var kvp in map)
		{
			var item = factory(kvp.Value);
			deserialized.Add(kvp.Key, item);
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
		string? s = null;
		if (n is not JsonValue v || !v.TryGetValue(out s) || !Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out var uri))
			throw new JsonException($"`{propertyName}` in {objectType} object must be a string containing a valid URI")
			{
				Data = { ["Value"] = s }
			};

		return uri;
	}

	public static Uri? MaybeUri(this JsonObject obj, string propertyName, string objectType)
	{
		if (!obj.TryGetPropertyValue(propertyName, out var n)) return null;
		string? s = null;
		if (n is not JsonValue v || !v.TryGetValue(out s) || !Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out var uri))
			throw new JsonException($"`{propertyName}` in {objectType} object must be a string containing a valid URI")
			{
				Data = { ["Value"] = s }
			};

		return uri;
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
		string? s = null;
		if (n is not JsonValue v || !v.TryGetValue(out s) || !Enum.TryParse(s, true, out T e))
			throw new JsonException($"`{propertyName}` in {objectType} object must be one of the predefined string values")
			{
				Data = { ["Value"] = s }
			};

		return e;
	}

	public static T? MaybeEnum<T>(this JsonObject obj, string propertyName, JsonSerializerOptions? options)
		where T : struct, Enum
	{
		if (!obj.TryGetPropertyValue(propertyName, out var n)) return null;			

		return n.Deserialize<T>(options);
	}

	public static void MaybeAdd(this JsonObject obj, string propertyName, JsonNode? value)
	{
		if (value == null) return;

		obj.Add(propertyName, value);
	}

	public static void AddExtensions(this JsonObject obj, ExtensionData? extensionData)
	{
		if (extensionData == null) return;

		foreach (var kvp in extensionData)
		{
			obj.Add(key, value?.DeepClone());
		}
	}

	public static void MaybeAddArray<T>(this JsonObject obj, string propertyName, IEnumerable<T>? values, Func<T, JsonNode?> convert)
	{
		if (values == null) return;

		obj.Add(propertyName, values.Select(convert).ToJsonArray());
	}

	public static void MaybeAddMap<T>(this JsonObject obj, string propertyName, Dictionary<string, T>? values, Func<T, JsonNode?> convert)
	{
		if (values == null) return;

		// We do this manually here because .ToDictionary() allocates an intermediate dictionary
		var newObj = new JsonObject();
		foreach (var kvp in values)
		{
			var node = convert(kvp.Value);
			newObj.Add(kvp.Key, node);
		}

		obj.Add(propertyName, newObj);
	}

	public static void MaybeAddEnum<T>(this JsonObject obj, string propertyName, T? value, JsonSerializerOptions? options)
		where T : struct, Enum
	{
		if (value == null) return;

		obj.Add(propertyName, JsonSerializer.SerializeToNode(value, options)!.GetValue<string>());
	}

	public static void MaybeSerialize<T>(this JsonObject obj, string propertyName, T? value, JsonSerializerOptions? options)
		where T : class
	{
		if (value == null) return;

		obj.Add(propertyName, JsonSerializer.SerializeToNode(value, options));
	}
}
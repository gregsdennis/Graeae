using System.Text.Json;
using System.Text.Json.Nodes;
using Json.More;
using Json.Schema;

namespace Graeae.Models;

internal static class SerializationExtensions
{
	public static JsonSchema? MaybeSchema(this JsonElement obj, string propertyName, BuildOptions options)
	{
		if (!obj.TryGetProperty(propertyName, out var value)) return null;
        return JsonSchema.Build(value, options);
    }

	public static T Expect<T>(this JsonElement obj, string propertyName, string objectType, Func<JsonElement, T> factory)
		where T : class
	{
		if (!obj.TryGetProperty(propertyName, out var value))
			throw new JsonException($"`{propertyName}` is required for {objectType} object");
		return factory(value);
	}

	public static T? Maybe<T>(this JsonElement obj, string propertyName, Func<JsonElement, T> factory)
		where T : class
	{
		if (!obj.TryGetProperty(propertyName, out var value)) return null;
		return factory(value);
	}

	public static IReadOnlyList<T>? MaybeArray<T>(this JsonElement obj, string propertyName, Func<JsonElement, T> factory)
	{
		if (!obj.TryGetProperty(propertyName, out var array)) return null;
		if (array.ValueKind is not JsonValueKind.Array)
			throw new JsonException($"Property `{propertyName}` must be an array");

		var deserialized = new List<T>();

		foreach (var value in array.EnumerateArray())
		{
			var item = factory(value);
			deserialized.Add(item);
		}

		return deserialized;
	}

	public static Dictionary<string, T> ExpectMap<T>(this JsonElement obj, string propertyName, string objectType, Func<JsonElement, T> factory)
	{
		if (!obj.TryGetProperty(propertyName, out var dict))
			throw new JsonException($"`{propertyName}` is required for {objectType} object");
		if (dict.ValueKind is not JsonValueKind.Object)
			throw new JsonException($"Property `{propertyName}` must be an object");

		var deserialized = new Dictionary<string, T>();

		foreach (var kvp in dict.EnumerateObject())
		{
			var item = factory(kvp.Value);
			deserialized.Add(kvp.Name, item);
		}

		return deserialized;
	}

	public static Dictionary<string, T>? MaybeMap<T>(this JsonElement obj, string propertyName, Func<JsonElement, T> factory)
	{
		if (!obj.TryGetProperty(propertyName, out var dict)) return null;
		if (dict.ValueKind is not JsonValueKind.Object)
			throw new JsonException($"Property `{propertyName}` must be an object");

		var deserialized = new Dictionary<string, T>();

		foreach (var kvp in dict.EnumerateObject())
		{
			var item = factory(kvp.Value);
			deserialized.Add(kvp.Name, item);
		}

		return deserialized;
	}

	public static string ExpectString(this JsonElement obj, string propertyName, string objectType)
	{
		if (!obj.TryGetProperty(propertyName, out var n))
			throw new JsonException($"`{propertyName}` is required for {objectType} object");
		if (n.ValueKind is not JsonValueKind.String)
			throw new JsonException($"`{propertyName}` in {objectType} object must be a string");

        return n.GetString()!;
	}

	public static string? MaybeString(this JsonElement obj, string propertyName, string objectType)
	{
		if (!obj.TryGetProperty(propertyName, out var n)) return null;			
		if (n.ValueKind is not JsonValueKind.String)
			throw new JsonException($"`{propertyName}` in {objectType} object must be a string");

		return n.GetString();
	}

	public static Uri ExpectUri(this JsonElement obj, string propertyName, string objectType)
	{
		if (!obj.TryGetProperty(propertyName, out var n))
			throw new JsonException($"`{propertyName}` is required for {objectType} object");
        if (n.ValueKind is not JsonValueKind.String)
            throw new JsonException($"`{propertyName}` in {objectType} object must be a string");
        var s = n.GetString();

		if (!Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out var uri))
			throw new JsonException($"`{propertyName}` in {objectType} object must be a string containing a valid URI")
			{
				Data = { ["Value"] = s }
			};

		return uri;
	}

	public static Uri? MaybeUri(this JsonElement obj, string propertyName, string objectType)
	{
		if (!obj.TryGetProperty(propertyName, out var n)) return null;
        if (n.ValueKind is not JsonValueKind.String)
            throw new JsonException($"`{propertyName}` in {objectType} object must be a string");
        var s = n.GetString();
		if (!Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out var uri))
			throw new JsonException($"`{propertyName}` in {objectType} object must be a string containing a valid URI")
			{
				Data = { ["Value"] = s }
			};

		return uri;
	}

	public static bool ExpectBool(this JsonElement obj, string propertyName, string objectType)
	{
		if (!obj.TryGetProperty(propertyName, out var n))
			throw new JsonException($"`{propertyName}` is required for {objectType} object");
		if (n.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
			throw new JsonException($"`{propertyName}` in {objectType} object must be a boolean");

		return n.GetBoolean();
	}

	public static bool? MaybeBool(this JsonElement obj, string propertyName, string objectType)
	{
		if (!obj.TryGetProperty(propertyName, out var n)) return null;
        if (n.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
			throw new JsonException($"`{propertyName}` in {objectType} object must be a boolean");

		return n.GetBoolean();
	}

	public static T ExpectEnum<T>(this JsonElement obj, string propertyName, string objectType)
		where T : struct, Enum
	{
		if (!obj.TryGetProperty(propertyName, out var n))
			throw new JsonException($"`{propertyName}` is required for {objectType} object");
        if (n.ValueKind is not JsonValueKind.String)
            throw new JsonException($"`{propertyName}` in {objectType} object must be a string");
        var s = n.GetString();
		if (!Enum.TryParse(s, true, out T e))
			throw new JsonException($"`{propertyName}` in {objectType} object must be one of the predefined string values")
			{
				Data = { ["Value"] = s }
			};

		return e;
	}

	public static T? MaybeEnum<T>(this JsonElement obj, string propertyName, string objectType)
		where T : struct, Enum
	{
		if (!obj.TryGetProperty(propertyName, out var n)) return null;
        if (n.ValueKind is not JsonValueKind.String)
            throw new JsonException($"`{propertyName}` in {objectType} object must be a string");
        var s = n.GetString();

        return !Enum.TryParse(s, true, out T e) ? null : e;
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
			obj.Add(kvp.Key, kvp.Value.AsNode());
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
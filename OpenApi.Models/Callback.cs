﻿using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OpenApi.Models;

[JsonConverter(typeof(CallbackJsonConverter))]
public class Callback : Dictionary<string, PathItem>, IRefResolvable
{
	public ExtensionData? ExtensionData { get; set; }

	public static Callback FromNode(JsonNode? node, JsonSerializerOptions? options)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		if (obj.ContainsKey("$ref"))
		{
			var callback = new CallbackRef(obj.ExpectUri("$ref", "reference"))
			{
				Description = obj.MaybeString("description", "reference"),
				Summary = obj.MaybeString("summary", "reference")
			};

			obj.ValidateReferenceKeys();

			return callback;
		}
		else
		{
			var callback = new Callback
			{
				ExtensionData = ExtensionData.FromNode(obj)
			};

			foreach (var (key, value) in obj)
			{
				if (key.StartsWith("x-")) continue;
				//callback.Add(RuntimeExpression.Parse(key), PathItem.FromNode(value, options));
				callback.Add(key, PathItem.FromNode(value, options));
			}

			// Validating extra keys is done in the loop.

			return callback;
		}
	}

	public static JsonNode? ToNode(Callback? callback, JsonSerializerOptions? options)
	{
		if (callback == null) return null;

		var obj = new JsonObject();

		foreach (var (key, value) in callback)
		{
			obj.Add(key, PathItem.ToNode(value, options));
		}

		obj.AddExtensions(callback.ExtensionData);

		return obj;
	}

	public object? Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return null;

		return this.GetFromMap(keys[0])?.Resolve(keys[1..]) ??
		       ExtensionData?.Resolve(keys);
	}
}

public class CallbackRef : Callback
{
	public Uri Ref { get; }
	public string? Summary { get; set; }
	public string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public CallbackRef(Uri reference)
	{
		Ref = reference ?? throw new ArgumentNullException(nameof(reference));
	}

	public void Resolve()
	{
		// resolve the $ref and set all of the props
		// remember to use base.Description

		IsResolved = true;
	}
}

public class CallbackJsonConverter : JsonConverter<Callback>
{
	public override Callback? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return Callback.FromNode(obj, options);
	}

	public override void Write(Utf8JsonWriter writer, Callback value, JsonSerializerOptions options)
	{
		var json = Callback.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}
﻿using Json.More;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OpenApi.Models;

[JsonConverter(typeof(ExampleJsonConverter))]
public class Example : IRefResolvable
{
	private static readonly string[] KnownKeys =
	{
		"summary",
		"description",
		"value",
		"externalValue"
	};

	public string? Summary { get; set; }
	public string? Description { get; set; }
	public JsonNode? Value { get; set; }
	public string? ExternalValue { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public static Example FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		if (obj.ContainsKey("$ref"))
		{
			var example = new ExampleRef(obj.ExpectUri("$ref", "reference"))
			{
				Description = obj.MaybeString("description", "reference"),
				Summary = obj.MaybeString("summary", "reference")
			};

			obj.ValidateReferenceKeys();

			return example;
		}
		else
		{
			var example = new Example
			{
				Summary = obj.MaybeString("summary", "example"),
				Description = obj.MaybeString("description", "example"),
				Value = obj.TryGetPropertyValue("value", out var v) ? v ?? JsonNull.SignalNode : null,
				ExternalValue = obj.MaybeString("externalValue", "example"),
				ExtensionData = ExtensionData.FromNode(obj)
			};

			obj.ValidateNoExtraKeys(KnownKeys, example.ExtensionData?.Keys);

			return example;
		}
	}

	public static JsonNode? ToNode(Example? example)
	{
		if (example == null) return null;

		var obj = new JsonObject();

		if (example is ExampleRef reference)
		{
			obj.Add("$ref", reference.Ref.ToString());
			obj.MaybeAdd("description", reference.Description);
			obj.MaybeAdd("summary", reference.Summary);
		}
		else
		{
			obj.MaybeAdd("summary", example.Summary);
			obj.MaybeAdd("description", example.Description);
			obj.MaybeAdd("value", example.Value.Copy());
			obj.MaybeAdd("externalValue", example.ExternalValue);
			obj.AddExtensions(example.ExtensionData);
		}

		return obj;
	}

	public object? Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		if (keys[0] == "value")
		{
			if (keys.Length == 1) return Value;
			keys[1..].ToPointer().TryEvaluate(Value, out var target);
			return target;
		}

		return ExtensionData?.Resolve(keys);
	}
}

public class ExampleRef : Example
{
	public Uri Ref { get; set; }
	public new string? Summary { get; set; }
	public new string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public ExampleRef(Uri reference)
	{
		Ref = reference ?? throw new ArgumentNullException(nameof(reference));
	}

	public void Resolve()
	{
		// resolve the $ref and set all of the props
		// remember to use base.*

		IsResolved = true;
	}
}

public class ExampleJsonConverter : JsonConverter<Example>
{
	public override Example? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return Example.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, Example value, JsonSerializerOptions options)
	{
		var json = Example.ToNode(value);

		JsonSerializer.Serialize(writer, json, options);
	}
}

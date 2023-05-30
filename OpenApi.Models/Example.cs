using Json.More;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenApi.Models;

public class Example
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
	public JsonNode? Value { get; set; } // use JsonNull
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

	public static JsonNode? ToNode(Example? example, JsonSerializerOptions? options)
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
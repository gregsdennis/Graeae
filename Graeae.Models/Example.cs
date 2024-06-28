using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.More;

namespace Graeae.Models;

/// <summary>
/// Models an example.
/// </summary>
[JsonConverter(typeof(ExampleJsonConverter))]
public class Example : IRefTargetContainer
{
	private static readonly string[] KnownKeys =
	{
		"summary",
		"description",
		"value",
		"externalValue"
	};

	/// <summary>
	/// Gets or sets the summary.
	/// </summary>
	public string? Summary { get; set; }
	/// <summary>
	/// Gets or sets the description.
	/// </summary>
	public string? Description { get; set; }
	/// <summary>
	/// Gets or sets the example value.
	/// </summary>
	public JsonNode? Value { get; set; }
	/// <summary>
	/// Gets or sets a URI that points to the literal example.
	/// </summary>
	public string? ExternalValue { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	internal static Example FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		Example example;
		if (obj.ContainsKey("$ref"))
		{
			example = new ExampleRef(obj.ExpectUri("$ref", "reference"))
			{
				Description = obj.MaybeString("description", "reference"),
				Summary = obj.MaybeString("summary", "reference")
			};

			obj.ValidateReferenceKeys();
		}
		else
		{
			example = new Example();
			example.Import(obj);

			obj.ValidateNoExtraKeys(KnownKeys, example.ExtensionData?.Keys);
		}
		
		return example;
	}

	private protected void Import(JsonObject obj)
	{
		Summary = obj.MaybeString("summary", "example");
		Description = obj.MaybeString("description", "example");
		Value = obj.TryGetPropertyValue("value", out var v) ? v : null;
		ExternalValue = obj.MaybeString("externalValue", "example");
		ExtensionData = ExtensionData.FromNode(obj);
	}

	internal static JsonNode? ToNode(Example? example)
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
			obj.MaybeAdd("value", example.Value?.DeepClone());
			obj.MaybeAdd("externalValue", example.ExternalValue);
			obj.AddExtensions(example.ExtensionData);
		}

		return obj;
	}

	object? IRefTargetContainer.Resolve(ReadOnlySpan<string> keys)
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

	internal IEnumerable<IComponentRef> FindRefs()
	{
		if (this is ExampleRef exRef)
			yield return exRef;
	}
}

/// <summary>
/// Models a `$ref` to an example.
/// </summary>
public class ExampleRef : Example, IComponentRef
{
	/// <summary>
	/// The URI for the reference.
	/// </summary>
	public Uri Ref { get; }

	/// <summary>
	/// Gets or sets the summary.
	/// </summary>
	public new string? Summary { get; set; }

	/// <summary>
	/// Gets or sets the description.
	/// </summary>
	public new string? Description { get; set; }

	/// <summary>
	/// Gets whether the reference has been resolved.
	/// </summary>
	public bool IsResolved { get; private set; }

	/// <summary>
	/// Creates a new <see cref="ExampleRef"/>
	/// </summary>
	/// <param name="reference">The reference URI</param>
	public ExampleRef(Uri reference)
	{
		Ref = reference ?? throw new ArgumentNullException(nameof(reference));
	}

	/// <summary>
	/// Creates a new <see cref="ExampleRef"/>
	/// </summary>
	/// <param name="reference">The reference URI</param>
	public ExampleRef(string reference)
	{
		Ref = new Uri(reference ?? throw new ArgumentNullException(nameof(reference)), UriKind.RelativeOrAbsolute);
	}

	async Task IComponentRef.Resolve(OpenApiDocument root, JsonSerializerOptions? options)
	{
		bool import(JsonNode? node)
		{
			if (node is not JsonObject obj) return false;

			Import(obj);
			return true;
		}

		void copy(Example other)
		{
			base.Summary = other.Summary;
			base.Description = other.Description;
			Value = other.Value;
			ExternalValue = other.ExternalValue;
			ExtensionData = other.ExtensionData;
		}

		IsResolved = await Models.Ref.Resolve<Example>(root, Ref, import, copy);
	}
}

internal class ExampleJsonConverter : JsonConverter<Example>
{
	public override Example Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

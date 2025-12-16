using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.More;
using Json.Schema;

namespace Graeae.Models;

/// <summary>
/// Models an example.
/// </summary>
[JsonConverter(typeof(ExampleJsonConverter))]
public class Example : IRefTargetContainer
{
	private static readonly string[] KnownKeys =
	[
		"summary",
		"description",
		"value",
		"externalValue"
	];

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
	public JsonElement? Value { get; set; }
	/// <summary>
	/// Gets or sets a URI that points to the literal example.
	/// </summary>
	public string? ExternalValue { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	internal static Example FromNode(JsonElement node)
	{
		if (node.ValueKind is not JsonValueKind.Object)
			throw new JsonException("Expected an object");

		Example example;
		if (node.TryGetProperty("$ref", out _))
		{
			example = new ExampleRef(node.ExpectUri("$ref", "reference"))
			{
				Description = node.MaybeString("description", "reference"),
				Summary = node.MaybeString("summary", "reference")
			};

            node.ValidateReferenceKeys();
		}
		else
		{
			example = new Example();
			example.Import(node);

            node.ValidateNoExtraKeys(KnownKeys, example.ExtensionData?.Keys);
		}
		
		return example;
	}

	private protected void Import(JsonElement obj)
	{
		Summary = obj.MaybeString("summary", "example");
		Description = obj.MaybeString("description", "example");
		Value = obj.TryGetProperty("value", out var v) ? v : null;
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
			obj.MaybeAdd("value", example.Value?.AsNode());
			obj.MaybeAdd("externalValue", example.ExternalValue);
			obj.AddExtensions(example.ExtensionData);
		}

		return obj;
	}

	object? IRefTargetContainer.Resolve(ReadOnlySpan<string> keys)
	{
		if (keys.Length == 0) return this;
        if (Value is null) return null;

		if (keys[0] == "value")
		{
			if (keys.Length == 1) return Value;
            var target = keys[1..].ToPointer().Evaluate(Value.Value);
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

	void IComponentRef.Resolve(OpenApiDocument root, BuildOptions buildOptions)
	{
		bool import(JsonElement? node)
		{
			if (node?.ValueKind is not JsonValueKind.Object) return false;

			Import(node.Value);
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

		IsResolved = Models.Ref.Resolve<Example>(root, Ref, import, copy);
	}
}

internal class ExampleJsonConverter : JsonConverter<Example>
{
	[RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(ref Utf8JsonReader, JsonSerializerOptions)")]
	[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
	public override Example Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
        if (obj.ValueKind is not JsonValueKind.Object)
            throw new JsonException("Expected an object");

		return Example.FromNode(obj);
	}

	[RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(ref Utf8JsonReader, JsonSerializerOptions)")]
	[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
	public override void Write(Utf8JsonWriter writer, Example value, JsonSerializerOptions options)
	{
		var json = Example.ToNode(value);

		JsonSerializer.Serialize(writer, json, options);
	}
}

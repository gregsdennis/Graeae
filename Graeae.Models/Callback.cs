using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Schema;

namespace Graeae.Models;

/// <summary>
/// Models a callback.
/// </summary>
[JsonConverter(typeof(CallbackJsonConverter))]
public class Callback : Dictionary<CallbackKeyExpression, PathItem>, IRefTargetContainer
{
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	internal static Callback FromNode(JsonElement node, BuildOptions buildOptions)
	{
		if (node.ValueKind is not JsonValueKind.Object)
			throw new JsonException("Expected an object");

		Callback callback;
		if (node.TryGetProperty("$ref", out _))
		{
			callback = new CallbackRef(node.ExpectUri("$ref", "reference"))
			{
				Description = node.MaybeString("description", "reference"),
				Summary = node.MaybeString("summary", "reference")
			};

            node.ValidateReferenceKeys();
		}
		else
		{
			callback = new Callback();
			callback.Import(node, buildOptions);
		}
		return callback;
	}

	private protected void Import(JsonElement obj, BuildOptions buildOptions)
	{
		ExtensionData = ExtensionData.FromNode(obj);

		foreach (var kvp in obj.EnumerateObject())
		{
			if (kvp.Name.StartsWith("x-")) continue;
			Add(CallbackKeyExpression.Parse(kvp.Name), PathItem.FromNode(kvp.Value, buildOptions));
		}
	}

	internal static JsonNode? ToNode(Callback? callback, JsonSerializerOptions? options)
	{
		if (callback == null) return null;

		var obj = new JsonObject();

		if (callback is CallbackRef reference)
		{
			obj.Add("$ref", reference.Ref.ToString());
			obj.MaybeAdd("description", reference.Description);
			obj.MaybeAdd("summary", reference.Summary);
		}
		else
		{
			foreach (var kvp in callback)
			{
				obj.Add(kvp.Key.ToString(), PathItem.ToNode(kvp.Value, options));
			}
			obj.AddExtensions(callback.ExtensionData);
		}

		return obj;
	}

	object? IRefTargetContainer.Resolve(ReadOnlySpan<string> keys)
	{
		if (keys.Length == 0) return null;

		return this.GetFromMap(keys[0])?.Resolve(keys.Slice(1)) ??
		       ExtensionData?.Resolve(keys);
	}

	internal IEnumerable<JsonSchema> FindSchemas()
	{
		return Values.SelectMany(x => x.FindSchemas());
	}

	internal IEnumerable<IComponentRef> FindRefs()
	{
		if (this is CallbackRef cRef)
			yield return cRef;

		var theRest = Values.SelectMany(x => x.FindRefs());

		foreach (var reference in theRest)
		{
			yield return reference;
		}
	}
}

/// <summary>
/// Models a `$ref` to a callback.
/// </summary>
public class CallbackRef : Callback, IComponentRef
{
	/// <summary>
	/// The URI for the reference.
	/// </summary>
	public Uri Ref { get; }

	/// <summary>
	/// Gets or sets the summary.
	/// </summary>
	public string? Summary { get; set; }

	/// <summary>
	/// Gets or sets the description.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// Gets whether the reference has been resolved.
	/// </summary>
	public bool IsResolved { get; private set; }

	/// <summary>
	/// Creates a new <see cref="CallbackRef"/>
	/// </summary>
	/// <param name="reference">The reference URI</param>
	public CallbackRef(Uri reference)
	{
		Ref = reference ?? throw new ArgumentNullException(nameof(reference));
	}

	/// <summary>
	/// Creates a new <see cref="CallbackRef"/>
	/// </summary>
	/// <param name="reference">The reference URI</param>
	public CallbackRef(string reference)
	{
		Ref = new Uri(reference ?? throw new ArgumentNullException(nameof(reference)), UriKind.RelativeOrAbsolute);
	}

	void IComponentRef.Resolve(OpenApiDocument root, BuildOptions buildOptions)
	{
		bool import(JsonElement? node)
		{
			if (node?.ValueKind is not JsonValueKind.Object) return false;

			Import(node.Value, buildOptions);
			return true;
		}

		void copy(Callback other)
		{
			ExtensionData = other.ExtensionData;
			foreach (var kvp in other)
			{
				this[kvp.Key] = kvp.Value;
			}
		}

		IsResolved = Models.Ref.Resolve<Callback>(root, Ref, import, copy);
	}
}

internal class CallbackJsonConverter : JsonConverter<Callback>
{
	public override Callback Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var obj = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
        if (obj.ValueKind is not JsonValueKind.Object)
            throw new JsonException("Expected an object");

		return Callback.FromNode(obj, BuildOptions.Default);
	}

	public override void Write(Utf8JsonWriter writer, Callback value, JsonSerializerOptions options)
	{
		var json = Callback.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}
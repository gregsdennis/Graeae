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

	internal static Callback FromNode(JsonNode? node, JsonSerializerOptions? options)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		Callback callback;
		if (obj.ContainsKey("$ref"))
		{
			callback = new CallbackRef(obj.ExpectUri("$ref", "reference"))
			{
				Description = obj.MaybeString("description", "reference"),
				Summary = obj.MaybeString("summary", "reference")
			};

			obj.ValidateReferenceKeys();
		}
		else
		{
			callback = new Callback();
			callback.Import(obj, options);
		}
		return callback;
	}

	private protected void Import(JsonObject obj, JsonSerializerOptions? options)
	{
		ExtensionData = ExtensionData.FromNode(obj);

		foreach (var kvp in obj)
		{
			if (key.StartsWith("x-")) continue;
			Add(CallbackKeyExpression.Parse(key), PathItem.FromNode(value, options));
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

	async Task IComponentRef.Resolve(OpenApiDocument root, JsonSerializerOptions? options)
	{
		bool import(JsonNode? node)
		{
			if (node is not JsonObject obj) return false;

			Import(obj, options);
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

		IsResolved = await Models.Ref.Resolve<Callback>(root, Ref, import, copy);
	}
}

internal class CallbackJsonConverter : JsonConverter<Callback>
{
	public override Callback Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
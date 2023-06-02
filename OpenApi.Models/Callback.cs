using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Schema;

namespace OpenApi.Models;

[JsonConverter(typeof(CallbackJsonConverter))]
public class Callback : Dictionary<CallbackKeyExpression, PathItem>, IRefTargetContainer
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
				callback.Add(CallbackKeyExpression.Parse(key), PathItem.FromNode(value, options));
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
			obj.Add(key.ToString(), PathItem.ToNode(value, options));
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

	public IEnumerable<JsonSchema> FindSchemas()
	{
		return Values.SelectMany(x => x.FindSchemas());
	}

	public IEnumerable<IComponentRef> FindRefs()
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

public class CallbackRef : Callback, IComponentRef
{
	public Uri Ref { get; }
	public string? Summary { get; set; }
	public string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public CallbackRef(Uri reference)
	{
		Ref = reference ?? throw new ArgumentNullException(nameof(reference));
	}

	public void Resolve(OpenApiDocument root)
	{
		// resolve the $ref and set all of the props

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
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Schema;

namespace Graeae.Models;

/// <summary>
/// Models a response collection.
/// </summary>
[JsonConverter(typeof(ResponseCollectionJsonConverter))]
public class ResponseCollection : Dictionary<HttpStatusCode, Response>, IRefTargetContainer
{
	/// <summary>
	/// Gets or sets the default response for the collection.
	/// </summary>
	public Response? Default { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	internal static ResponseCollection FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var collection = new ResponseCollection
		{
			Default = obj.Maybe("default", Response.FromNode),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		foreach (var (key, value) in obj)
		{
			if (key == "default") continue;
			if (key.StartsWith("x-")) continue;
			if (!short.TryParse(key, out var code))
				throw new JsonException($"`{key}` is not a valid status code");
			if (Enum.GetName(typeof(HttpStatusCode), code) == null)
				throw new JsonException($"`{key}` is not a known status code");

			collection.Add((HttpStatusCode)code, Response.FromNode(value));
		}

		// Validating extra keys is done in the loop.

		return collection;
	}

	internal static JsonNode? ToNode(ResponseCollection? responses, JsonSerializerOptions? options)
	{
		if (responses == null) return null;

		var obj = new JsonObject();

		foreach (var (key, value) in responses)
		{
			obj.Add(((int)key).ToString(), Response.ToNode(value, options));
		}

		obj.MaybeAdd("default", Response.ToNode(responses.Default, options));

		obj.AddExtensions(responses.ExtensionData);

		return obj;
	}

	object? IRefTargetContainer.Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return null;

		var first = keys[0];
		return this.FirstOrDefault(x => ((int)x.Key).ToString() == first).Value?.Resolve(keys[1..]) ??
		       ExtensionData?.Resolve(keys);
	}

	internal IEnumerable<JsonSchema> FindSchemas()
	{
		return GeneralHelpers.Collect(
			Default?.FindSchemas(),
			Values.SelectMany(x => x.FindSchemas())
		);
	}

	internal IEnumerable<IComponentRef> FindRefs()
	{
		if (Default is ResponseRef rRef)
			yield return rRef;

		var theRest = Values.SelectMany(x => x.FindRefs());

		foreach (var reference in theRest)
		{
			yield return reference;
		}
	}
}

internal class ResponseCollectionJsonConverter : JsonConverter<ResponseCollection>
{
	public override ResponseCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return ResponseCollection.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, ResponseCollection value, JsonSerializerOptions options)
	{
		var json = ResponseCollection.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}
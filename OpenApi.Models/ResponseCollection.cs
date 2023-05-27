using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenApi.Models;

public class ResponseCollection : Dictionary<HttpStatusCode, Response>
{
	public Response? Default { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public static ResponseCollection FromNode(JsonNode? node, JsonSerializerOptions? options)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var collection = new ResponseCollection
		{
			Default = obj.Maybe("default", x=> Response.FromNode(x, options)),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		foreach (var (key, value) in obj)
		{
			if (key.StartsWith("x-")) continue;
			if (!short.TryParse(key, out var code))
				throw new JsonException($"`{key}` is not a valid status code");
			if (Enum.GetName(typeof(HttpStatusCode), code) == null)
				throw new JsonException($"`{key}` is not a known status code");

			collection.Add((HttpStatusCode)code, Response.FromNode(value, options));
		}

		// Validating extra keys is done in the loop.

		return collection;
	}
}
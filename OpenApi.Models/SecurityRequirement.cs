using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenApi.Models;

public class SecurityRequirement : Dictionary<string, IEnumerable<string>>
{
	public static SecurityRequirement FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var callback = new SecurityRequirement();

		foreach (var (key, value) in obj)
		{
			if (value is not JsonArray array)
				throw new JsonException("security requirements must be string arrays");
			

			callback.Add(key, array.Select(x => x is JsonValue v && v.TryGetValue(out string? s) ? s : throw new JsonException("security requirement values must be strings")));
		}

		// Validating extra keys is done in the loop.

		return callback;
	}
}
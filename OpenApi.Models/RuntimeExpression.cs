using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenApi.Models;

public class RuntimeExpression
{
	// see https://spec.openapis.org/oas/v3.1.0#runtime-expressions

	public static RuntimeExpression FromNode(JsonNode? node, JsonSerializerOptions? options)
	{
		if (node is not JsonValue value || !value.TryGetValue(out string? source))
			throw new JsonException("runtime expressions must be strings");

		throw new NotImplementedException();
	}
}
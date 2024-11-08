﻿using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.More;

namespace Graeae.Models;

/// <summary>
/// Models a security requirement.
/// </summary>
[JsonConverter(typeof(SecurityRequirementJsonConverter))]
public class SecurityRequirement : Dictionary<string, IEnumerable<string>>
{
	internal static SecurityRequirement FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var callback = new SecurityRequirement();

		foreach (var kvp in obj)
		{
			if (kvp.Value is not JsonArray array)
				throw new JsonException("security requirements must be string arrays");
			

			callback.Add(kvp.Key, array.Select(x => x is JsonValue v && v.TryGetValue(out string? s) ? s : throw new JsonException("security requirement values must be strings")));
		}

		// Validating extra keys is done in the loop.

		return callback;
	}

	internal static JsonNode? ToNode(SecurityRequirement? requirement)
	{
		if (requirement == null) return null;

		var obj = new JsonObject();

		foreach (var kvp in requirement)
		{
			obj.Add(kvp.Key, kvp.Value.Select(x => (JsonNode?)x).ToJsonArray());
		}

		return obj;
	}
}

internal class SecurityRequirementJsonConverter : JsonConverter<SecurityRequirement>
{
	public override SecurityRequirement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return SecurityRequirement.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, SecurityRequirement value, JsonSerializerOptions options)
	{
		var json = SecurityRequirement.ToNode(value);

		JsonSerializer.Serialize(writer, json, options);
	}
}
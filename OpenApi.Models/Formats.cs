using System.Text.Json.Nodes;
using Json.Schema;

namespace OpenApi.Models;

public static class Formats
{
	public static readonly Format Int32 = new PredicateFormat("int32", ValidateInt32);

	private static bool ValidateInt32(JsonNode? element)
	{
		return element is JsonValue v && v.TryGetValue(out int _);
	}

	public static readonly Format Int64 = new PredicateFormat("int64", ValidateInt64);

	private static bool ValidateInt64(JsonNode? element)
	{
		return element is JsonValue v && v.TryGetValue(out long _);
	}

	public static readonly Format Float = new PredicateFormat("float", ValidateFloat);

	private static bool ValidateFloat(JsonNode? element)
	{
		return element is JsonValue v && v.TryGetValue(out float _);
	}

	public static readonly Format Double = new PredicateFormat("double", ValidateDouble);

	private static bool ValidateDouble(JsonNode? element)
	{
		return element is JsonValue v && v.TryGetValue(out double _);
	}

	public static readonly Format Password = new("password");
}
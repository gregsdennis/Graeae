using System.Text.Json;
using Json.Schema;

namespace Graeae.Models;

/// <summary>
/// Provides extended JSON Schema formats.
/// </summary>
public static class Formats
{
	/// <summary>
	/// Validates that a number is a valid 32-bit integer.
	/// </summary>
	public static readonly Format Int32 = new PredicateFormat("int32", ValidateInt32);

	private static bool ValidateInt32(JsonElement element)
	{
		return element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int _);
	}

	/// <summary>
	/// Validates that a number is a valid 64-bit integer.
	/// </summary>
	public static readonly Format Int64 = new PredicateFormat("int64", ValidateInt64);

	private static bool ValidateInt64(JsonElement element)
	{
        return element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out long _);
	}

    /// <summary>
    /// Validates that a number is a valid single-precision floating point value.
    /// </summary>
    public static readonly Format Float = new PredicateFormat("float", ValidateFloat);

	private static bool ValidateFloat(JsonElement element)
	{
        return element.ValueKind == JsonValueKind.Number && element.TryGetSingle(out float _);
	}

    /// <summary>
    /// Validates that a number is a valid double-precision floating point value.
    /// </summary>
    public static readonly Format Double = new PredicateFormat("double", ValidateDouble);

	private static bool ValidateDouble(JsonElement element)
	{
        return element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out double _);
	}

    /// <summary>
    /// Validates that a string is a password.
    /// </summary>
    public static readonly Format Password = new("password");
}	
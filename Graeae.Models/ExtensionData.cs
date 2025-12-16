using System.Text.Json;

namespace Graeae.Models;

/// <summary>
/// Supports extension data for all types.
/// </summary>
public class ExtensionData : Dictionary<string, JsonElement>, IRefTargetContainer
{
	internal static ExtensionData? FromNode(JsonElement obj)
	{
		var data = new ExtensionData();
		foreach (var kvp in obj.EnumerateObject().Where(x => x.Name.StartsWith("x-")))
		{
			data.Add(kvp.Name, kvp.Value);
		}

		return data.Any() ? data : null;
	}

	object? IRefTargetContainer.Resolve(ReadOnlySpan<string> keys)
	{
		if (keys.Length == 0)
			throw new InvalidOperationException("Greg forgot to check for an empty span.");

		if (!TryGetValue(keys[0], out var jn)) return null;
		if (keys.Length == 1) return jn;

		var result = keys[1..].ToPointer().Evaluate(jn);

		return result;
	}
}

using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenApi.Models;

internal static class ValidationHelper
{
	private static readonly string[] ReferenceKeys =
	{
		"$ref",
		"summary",
		"description"
	};

	public static void ValidateNoExtraKeys(this JsonObject obj, IEnumerable<string> knownKeys, IEnumerable<string>? extensionKeys = null)
	{
		// ReSharper disable PossibleMultipleEnumeration
		if (extensionKeys != null)
			knownKeys = knownKeys.Concat(extensionKeys);
		var extraKeys = ((IDictionary<string, JsonNode?>)obj).Keys.Except(knownKeys);
		if (extraKeys.Any())
			throw new JsonException("Extra keys are not supported.")
			{
				Data = { ["extraKeys"] = extraKeys.ToArray() }
			};
		// ReSharper restore PossibleMultipleEnumeration
	}

	public static void ValidateReferenceKeys(this JsonObject obj)
	{
		obj.ValidateNoExtraKeys(ReferenceKeys);
	}
}
using System.Text.Json;

namespace Graeae.Models;

internal static class ValidationHelper
{
	private static readonly string[] ReferenceKeys =
	{
		"$ref",
		"summary",
		"description"
	};

	public static void ValidateNoExtraKeys(this JsonElement obj, IEnumerable<string> knownKeys, IEnumerable<string>? extensionKeys = null)
	{
		// ReSharper disable PossibleMultipleEnumeration
		if (extensionKeys != null)
			knownKeys = knownKeys.Concat(extensionKeys);
		var extraKeys = obj.EnumerateObject().Select(x => x.Name).Except(knownKeys);
		if (extraKeys.Any())
			throw new JsonException("Extra keys are not supported.")
			{
				Data = { ["extraKeys"] = extraKeys.ToArray() }
			};
		// ReSharper restore PossibleMultipleEnumeration
	}

	public static void ValidateReferenceKeys(this JsonElement obj)
	{
		obj.ValidateNoExtraKeys(ReferenceKeys);
	}
}
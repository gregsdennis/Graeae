namespace OpenApi.Models;

internal static class ParsingHelper
{
	public static string Expect(this string source, ref int i, params string[] options)
	{
		foreach (var option in options)
		{
			if (source[i..].StartsWith(option))
			{
				i += option.Length;
				return option;
			}
		}

		throw new ArgumentOutOfRangeException(nameof(options), "None of the options were found");
	}
}
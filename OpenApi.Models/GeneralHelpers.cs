namespace OpenApi.Models;

internal static class GeneralHelpers
{
	public static IEnumerable<T> Collect<T>(params IEnumerable<T>?[] collections)
	{
		return collections.Where(x => x != null).SelectMany(x => x!);
	}
}
using System.Text.RegularExpressions;

namespace Graeae.AspNet.Analyzer;

internal static class PathHelpers
{
	public static readonly Regex TemplatedSegmentPattern = new(@"^\{(?<param>.*)\}$", RegexOptions.Compiled | RegexOptions.ECMAScript);
}
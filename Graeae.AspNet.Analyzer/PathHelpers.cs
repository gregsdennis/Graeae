using Corvus.Json;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;

namespace Graeae.AspNet.Analyzer;

internal static class PathHelpers
{
	public static readonly Regex TemplatedSegmentPattern = new(@"^\{(?<param>.*)\}$", RegexOptions.Compiled | RegexOptions.ECMAScript);

	//public static string Normalize(string path) => path.Replace("\\", "/");
	public static string Normalize(string path) => new Uri(path).ToString().Replace("file:///C:", "https://graeae.net");
}
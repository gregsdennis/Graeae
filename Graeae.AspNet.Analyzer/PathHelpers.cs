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

	public static bool TryNormalizeSchemaReference(string schemaFile, [NotNullWhen(true)] out string? result)
	{
		var uri = new JsonUri(schemaFile);
		if (!uri.IsValid() || uri.GetUri().IsFile)
		{
			// If this is, in fact, a local file path, not a uri, then convert to a fullpath and URI-style separators.
			if (IsPartiallyQualified(schemaFile.AsSpan()))
			{
				schemaFile = Path.GetFullPath(schemaFile);
			}

			result = schemaFile.Replace('\\', '/');
			return true;
		}

		result = null;
		return false;

		// <licensing>
		// Licensed to the .NET Foundation under one or more agreements.
		// The .NET Foundation licenses this file to you under the MIT license.
		// </licensing>
		static bool IsPartiallyQualified(ReadOnlySpan<char> path)
		{
			if (path.Length < 2)
			{
				// It isn't fixed, it must be relative.  There is no way to specify a fixed
				// path with one character (or less).
				return true;
			}

			if (IsDirectorySeparator(path[0]))
			{
				// There is no valid way to specify a relative path with two initial slashes or
				// \? as ? isn't valid for drive relative paths and \??\ is equivalent to \\?\
				return !(path[1] == '?' || IsDirectorySeparator(path[1]));
			}

			// The only way to specify a fixed path that doesn't begin with two slashes
			// is the drive, colon, slash format- i.e. C:\
			return !((path.Length >= 3)
				&& (path[1] == Path.VolumeSeparatorChar)
				&& IsDirectorySeparator(path[2])

				// To match old behavior we'll check the drive character for validity as the path is technically
				// not qualified if you don't have a valid drive. "=:\" is the "=" file's default data stream.
				&& IsValidDriveChar(path[0]));
		}

		static bool IsDirectorySeparator(char c)
		{
			return c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
		}

		static bool IsValidDriveChar(char value)
		{
			return (uint)((value | 0x20) - 'a') <= (uint)('z' - 'a');
		}
	}
}
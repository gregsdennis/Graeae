using System.Text.RegularExpressions;
using Json.Pointer;

namespace OpenApi.Models;

public class PathTemplate : IEquatable<PathTemplate>
{
	// /path/{item} and /path/{otherItem} are equal
	// /path/{item} and /{path}/item are not equal, but are ambiguous

	private static readonly Regex TemplatedSegmentPattern = new(@"^\{.*\}$", RegexOptions.Compiled | RegexOptions.ECMAScript);

	public string[] Segments { get; }

	public PathTemplate(string[] segments)
	{
		Segments = segments;
	}

	public static bool TryParse(string source, out PathTemplate template)
	{
		var asPointer = JsonPointer.Parse(source);

		template = new PathTemplate(asPointer.Segments.Select(x => x.Value).ToArray());

		return true;
	}

	public override string ToString()
	{
		return string.Concat(Segments.Select(x => $"/{x}"));
	}

	public bool Equals(PathTemplate? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;

		if (Segments.Length != other.Segments.Length) return false;

		var zipped = Segments.Zip(other.Segments);

		return zipped.All(x => x.First == x.Second ||
		                       TemplatedSegmentPattern.IsMatch(x.First) && TemplatedSegmentPattern.IsMatch(x.Second));
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as PathTemplate);
	}

	public override int GetHashCode()
	{
		int result = 0;
		unchecked
		{
			foreach (var segment in Segments)
			{
				if (TemplatedSegmentPattern.IsMatch(segment)) result += 1;

				result += segment.GetHashCode();
			}
		}
		return result;
	}
}
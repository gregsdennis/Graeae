namespace OpenApi.Models;

public class PathTemplate
{
	// will use PathParameter

	// need to be iequatable
	// /path/{item} and /path/{otherItem} are equal
	// /path/{item} and /{path}/item are not equal, but are ambiguous
	public static bool TryParse(string source, out PathTemplate template)
	{
		throw new NotImplementedException();
	}
}
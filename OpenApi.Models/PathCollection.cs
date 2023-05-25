namespace OpenApi.Models;

public class PathCollection : Dictionary<PathTemplate, PathItem>
{
	public ExtensionData? ExtensionData { get; set; }
}
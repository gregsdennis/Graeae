using Graeae.Models;

namespace Graeae.AspNet;

[AttributeUsage(AttributeTargets.Class)]
public class RequestHandlerAttribute : Attribute
{
	public string Path { get; }

	public RequestHandlerAttribute(string path)
	{
		if (!PathTemplate.TryParse(path, out _))
			throw new ArgumentException($"'{path}' is not a valid path specifier.");

		Path = path;
	}
}
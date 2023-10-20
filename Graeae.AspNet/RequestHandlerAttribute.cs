using Graeae.Models;

namespace Graeae.AspNet;

/// <summary>
/// Indicates that the attributed class contains handler methods for the indicated route.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class RequestHandlerAttribute : Attribute
{
	/// <summary>
	/// The route to handle.
	/// </summary>
	public string Path { get; }

	/// <summary>
	/// Creates a new <see cref="RequestHandlerAttribute"/>
	/// </summary>
	/// <param name="path">The route to handle</param>
	/// <exception cref="ArgumentException">Thrown when the route is not a valid path template</exception>
	public RequestHandlerAttribute(string path)
	{
		if (!PathTemplate.TryParse(path, out _))
			throw new ArgumentException($"'{path}' is not a valid path template.");

		Path = path;
	}
}
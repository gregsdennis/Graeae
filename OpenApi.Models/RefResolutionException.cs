namespace OpenApi.Models;

/// <summary>
/// Thrown when a `$ref` cannot be resolved.
/// </summary>
public class RefResolutionException : Exception
{
	public RefResolutionException(string message)
		: base(message)
	{
	}
}
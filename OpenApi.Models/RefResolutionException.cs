namespace OpenApi.Models;

public class RefResolutionException : Exception
{
	public RefResolutionException(string message)
		: base(message)
	{
	}
}
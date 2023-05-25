namespace OpenApi.Models;

public class Server
{
	public static Server Default { get; } = new(new Uri("/"));

	public Uri Url { get; }
	public string? Description { get; set; }
	public Dictionary<string, ServerVariable>? Variables { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public Server(Uri url)
	{
		Url = url ?? throw new ArgumentNullException(nameof(url));
	}
}
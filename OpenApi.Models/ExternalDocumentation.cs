namespace OpenApi.Models;

public class ExternalDocumentation
{
	public string? Description { get; set; }
	public Uri Url { get; }
	public ExtensionData? ExtensionData { get; set; }

	public ExternalDocumentation(Uri url)
	{
		Url = url ?? throw new ArgumentNullException(nameof(url));
	}
}
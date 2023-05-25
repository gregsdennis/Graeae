namespace OpenApi.Models;

public class ContactInfo
{
	public string? Name { get; set; }
	public Uri? Url { get; set; }
	public string? Email { get; set; }
	public ExtensionData? ExtensionData { get; set; }
}
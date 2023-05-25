namespace OpenApi.Models;

public class OpenApiInfo
{
	public string Title { get; set; }
	public string? Summary { get; set; }
	public string? Description { get; set; }
	public string? TermsOfService { get; set; }
	public ContactInfo? Contact { get; set; }
	public LicenseInfo? License { get; set; }
	public string Version { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public OpenApiInfo(string title, string version)
	{
		Title = title ?? throw new ArgumentNullException(nameof(title));
		Version = version ?? throw new ArgumentNullException(nameof(version));
	}
}
namespace OpenApi.Models;

public class LicenseInfo
{
	public string Name { get; }
	public string? Identifier { get; set; }
	public Uri? Url { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public LicenseInfo(string name)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
	}
}
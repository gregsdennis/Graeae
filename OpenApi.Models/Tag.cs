namespace OpenApi.Models;

public class Tag
{
	public string Name { get; }
	public string? Description { get; set; }
	public ExternalDocumentation? ExternalDocs { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public Tag(string name)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
	}
}
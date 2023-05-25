namespace OpenApi.Models;

public class ServerVariable
{
	public IEnumerable<string>? Enum { get; set; }
	public string Default { get; }
	public string? Description { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public ServerVariable(string defaultValue)
	{
		Default = defaultValue ?? throw new ArgumentNullException(nameof(defaultValue));
	}
}
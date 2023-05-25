namespace OpenApi.Models;

public class Encoding
{
	public string? ContentType { get; set; }
	public Dictionary<string, Header>? Headers { get; set; }
	public ParameterStyle? Style { get; set; }
	public bool? Explode { get; set; }
	public bool? AllowReserved { get; set; }
	public ExtensionData? ExtensionData { get; set; }
}
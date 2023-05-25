namespace OpenApi.Models;

public class Operation
{
	public IEnumerable<string>? Tags { get; set; }
	public string? Summary { get; set; }
	public string? Description { get; set; }
	public ExternalDocumentation? ExternalDocs { get; set; }
	public string? OperationId { get; set; }
	public IEnumerable<Parameter>? Parameters { get; set; }
	public RequestBody? RequestBody { get; set; }
	public ResponseCollection? Responses { get; set; }
	public Dictionary<string, Callback>? Callbacks { get; set; }
	public bool? Deprecated { get; set; }
	public IEnumerable<SecurityRequirement>? Security { get; set; }
	public IEnumerable<Server>? Servers { get; set; }
	public ExtensionData? ExtensionData { get; set; }
}
using Json.Schema;

namespace OpenApi.Models;

public class ComponentCollection
{
	public Dictionary<string, JsonSchema>? Schemas { get; set; }
	public Dictionary<string, Response>? Responses { get; set; }
	public Dictionary<string, Parameter>? Parameters { get; set; }
	public Dictionary<string, Example>? Examples { get; set; }
	public Dictionary<string, RequestBody>? RequestBodies { get; set; }
	public Dictionary<string, Header>? Headers { get; set; }
	public Dictionary<string, SecurityScheme>? SecuritySchemas { get; set; }
	public Dictionary<string, Link>? Links { get; set; }
	public Dictionary<string, Callback>? Callbacks { get; set; }
	public Dictionary<string, PathItem>? PathItems { get; set; }
	public ExtensionData? ExtensionData { get; set; }
}
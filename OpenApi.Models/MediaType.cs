using System.Text.Json.Nodes;
using Json.Schema;

namespace OpenApi.Models;

public class MediaType
{
	public JsonSchema? Schema { get; set; }
	public JsonNode? Example { get; set; } // use JsonNull
	public Dictionary<string, Example>? Examples { get; set; }
	public Dictionary<string, Encoding>? Encoding { get; set; }
	public ExtensionData? ExtensionData { get; set; }
}
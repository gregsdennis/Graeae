using System.Text.Json.Nodes;

namespace OpenApi.Models;

public class ExtensionData : Dictionary<string, JsonNode?>
{
	// keys MUST be x-*
}
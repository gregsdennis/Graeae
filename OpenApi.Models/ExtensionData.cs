using System.Text.Json.Nodes;

namespace OpenApi.Models;

public class ExtensionData : Dictionary<string, JsonNode?>
{
	public static ExtensionData? FromNode(JsonObject obj)
	{
		var data = new ExtensionData();
		foreach (var (key, value) in obj.Where(x => x.Key.StartsWith("x-")))
		{
			data.Add(key, value);
		}

		return data.Any() ? data : null;
	}
}

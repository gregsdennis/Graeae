using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenApi.Models;

public class Server
{
	private static readonly string[] KnownKeys =
	{
		"uri",
		"description",
		"variables"
	};

	public static Server Default { get; } = new() { Url = new Uri("/") };

	public Uri Url { get; set; }
	public string? Description { get; set; }
	public Dictionary<string, ServerVariable>? Variables { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public static Server FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var server = new Server
		{
			Url = obj.ExpectUri("uri", "server"),
			Description = obj.MaybeString("description", "server"),
			Variables = obj.MaybeMap("variables", x => ServerVariable.FromNode(x)),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, server.ExtensionData?.Keys);

		return server;
	}
}
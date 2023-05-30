using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenApi.Models;

public class Server
{
	private static readonly string[] KnownKeys =
	{
		"url",
		"description",
		"variables"
	};

	public static Server Default { get; } = new() { Url = "/" };

	public string Url { get; set; } // may include variables
	public string? Description { get; set; }
	public Dictionary<string, ServerVariable>? Variables { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public static Server FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var server = new Server
		{
			Url = obj.ExpectString("url", "server"),
			Description = obj.MaybeString("description", "server"),
			Variables = obj.MaybeMap("variables", ServerVariable.FromNode),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, server.ExtensionData?.Keys);

		return server;
	}

	public static JsonNode? ToNode(Server? server, JsonSerializerOptions? options)
	{
		if (server == null) return null;

		var obj = new JsonObject
		{
			["url"] = server.Url
		};

		obj.MaybeAdd("description", server.Description);
		obj.MaybeAddMap("variables", server.Variables, x => ServerVariable.ToNode(x, options));
		obj.AddExtensions(server.ExtensionData);

		return obj;
	}
}
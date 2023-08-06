using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OpenApi.Models;

/// <summary>
/// Models a server.
/// </summary>
[JsonConverter(typeof(ServerJsonConverter))]
public class Server : IRefTargetContainer
{
	private static readonly string[] KnownKeys =
	{
		"url",
		"description",
		"variables"
	};

	/// <summary>
	/// Gets the URL of the server.
	/// </summary>
	public string Url { get; } // may include variables
	/// <summary>
	/// Gets or sets the description.
	/// </summary>
	public string? Description { get; set; }
	/// <summary>
	/// Gets or sets the variable map.
	/// </summary>
	public Dictionary<string, ServerVariable>? Variables { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	/// <summary>
	/// Creates a new <see cref="Server"/>
	/// </summary>
	/// <param name="url">The server URL</param>
	public Server(string url)
	{
		Url = url;
	}

	internal static Server FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var server = new Server(obj.ExpectString("url", "server"))
		{
			Description = obj.MaybeString("description", "server"),
			Variables = obj.MaybeMap("variables", ServerVariable.FromNode),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, server.ExtensionData?.Keys);

		return server;
	}

	internal static JsonNode? ToNode(Server? server)
	{
		if (server == null) return null;

		var obj = new JsonObject
		{
			["url"] = server.Url
		};

		obj.MaybeAdd("description", server.Description);
		obj.MaybeAddMap("variables", server.Variables, ServerVariable.ToNode);
		obj.AddExtensions(server.ExtensionData);

		return obj;
	}

	object? IRefTargetContainer.Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		if (keys[0] == "variables")
		{
			if (keys.Length == 1) return null;
			return Variables.GetFromMap(keys[1])?.Resolve(keys[2..]);
		}

		return ExtensionData?.Resolve(keys);
	}
}

internal class ServerJsonConverter : JsonConverter<Server>
{
	public override Server Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return Server.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, Server value, JsonSerializerOptions options)
	{
		var json = Server.ToNode(value);

		JsonSerializer.Serialize(writer, json, options);
	}
}
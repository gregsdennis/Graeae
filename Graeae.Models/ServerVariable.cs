using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Graeae.Models;

/// <summary>
/// Models a server variable.
/// </summary>
[JsonConverter(typeof(ServerVariableJsonConverter))]
public class ServerVariable : IRefTargetContainer
{
	private static readonly string[] KnownKeys =
	{
		"enum",
		"default",
		"description"
	};

	/// <summary>
	/// Gets or sets an enumeration of string values to be used if the substitution options are from a limited set.
	/// </summary>
	public IReadOnlyList<string>? Enum { get; set; }
	/// <summary>
	/// Gets the default value to use for substitution.
	/// </summary>
	public string Default { get; }
	/// <summary>
	/// Gets or sets the description.
	/// </summary>
	public string? Description { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	/// <summary>
	/// Creates a new <see cref="ServerVariable"/>
	/// </summary>
	/// <param name="default">The default value</param>
	public ServerVariable(string @default)
	{
		Default = @default;
	}

	internal static ServerVariable FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var vars = new ServerVariable(obj.ExpectString("default", "server variable"))
		{
			Enum = obj.MaybeArray("enum", x => x is JsonValue v && v.TryGetValue(out string? s) ? s : throw new JsonException("`enum` values must be strings")),
			Description = obj.MaybeString("description", "server variable"),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, vars.ExtensionData?.Keys);

		return vars;
	}

	internal static JsonNode? ToNode(ServerVariable? variable)
	{
		if (variable == null) return null;

		var obj = new JsonObject
		{
			["default"] = variable.Default
		};

		obj.MaybeAddArray("enum", variable.Enum, x => x);
		obj.MaybeAdd("description", variable.Description);
		obj.AddExtensions(variable.ExtensionData);

		return obj;
	}

	object? IRefTargetContainer.Resolve(ReadOnlySpan<string> keys)
	{
		if (keys.Length == 0) return this;

		return ExtensionData?.Resolve(keys);
	}
}

internal class ServerVariableJsonConverter : JsonConverter<ServerVariable>
{
	public override ServerVariable Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return ServerVariable.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, ServerVariable value, JsonSerializerOptions options)
	{
		var json = ServerVariable.ToNode(value);

		JsonSerializer.Serialize(writer, json, options);
	}
}
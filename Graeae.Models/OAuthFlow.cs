using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Graeae.Models;

/// <summary>
/// Models an OAuth flow.
/// </summary>
[JsonConverter(typeof(OAuthFlowJsonConverter))]
public class OAuthFlow : IRefTargetContainer
{
	private static readonly string[] KnownKeys =
	{
		"authorizationUrl",
		"tokenUrl",
		"refreshUrl",
		"scopes"
	};

	/// <summary>
	/// Gets the authorization URL.
	/// </summary>
	public Uri? AuthorizationUrl { get; set; }
	/// <summary>
	/// Gets the token URL.
	/// </summary>
	public Uri? TokenUrl { get; set; }
	/// <summary>
	/// Gets or sets the refresh token URL.
	/// </summary>
	public Uri? RefreshUrl { get; set; }
	/// <summary>
	/// Gets the scopes.
	/// </summary>
	public Dictionary<string, string> Scopes { get; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	/// <summary>
	/// Creates a new <see cref="OAuthFlow"/>
	/// </summary>
	/// <param name="scopes">The scopes</param>
	public OAuthFlow(Dictionary<string, string> scopes)
	{
		Scopes = scopes;
	}

	internal static OAuthFlow FromNode(JsonElement node)
	{
		if (node.ValueKind is not JsonValueKind.Object)
			throw new JsonException("Expected an object");

		var flow = new OAuthFlow(
            node.ExpectMap("scopes", "oauth flow", x => x.ValueKind is JsonValueKind.String ? x.GetString()! : throw new JsonException("scopes must be strings")))
		{
            AuthorizationUrl = node.MaybeUri("authorizationUrl", "oauth flow"),
            TokenUrl = node.MaybeUri("tokenUrl", "oauth flow"),
			RefreshUrl = node.MaybeUri("refreshUrl", "oauth flow"),
			ExtensionData = ExtensionData.FromNode(node)
		};

        node.ValidateNoExtraKeys(KnownKeys, flow.ExtensionData?.Keys);

		return flow;
	}

	internal static JsonNode? ToNode(OAuthFlow? flow)
	{
		if (flow == null) return null;

		var obj = new JsonObject();
		obj.MaybeAdd("authorizationUrl", flow.AuthorizationUrl?.ToString());
		obj.MaybeAdd("tokenUrl", flow.TokenUrl?.ToString());
		obj.MaybeAdd("refreshUrl", flow.RefreshUrl?.ToString());

		var scopes = new JsonObject();
		foreach (var kvp in flow.Scopes)
		{
			scopes.Add(kvp.Key, kvp.Value);
		}
		obj.Add("scopes", scopes);

		obj.AddExtensions(flow.ExtensionData);

		return obj;
	}

	object? IRefTargetContainer.Resolve(ReadOnlySpan<string> keys)
	{
		if (keys.Length == 0) return this;

		return ExtensionData?.Resolve(keys);
	}
}

internal class OAuthFlowJsonConverter : JsonConverter<OAuthFlow>
{
	public override OAuthFlow Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var obj = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
        if (obj.ValueKind is not JsonValueKind.Object)
            throw new JsonException("Expected an object");

		return OAuthFlow.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, OAuthFlow value, JsonSerializerOptions options)
	{
		var json = OAuthFlow.ToNode(value);

		JsonSerializer.Serialize(writer, json, options);
	}
}

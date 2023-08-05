using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OpenApi.Models;

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

	public Uri AuthorizationUrl { get; }
	public Uri TokenUrl { get; }
	public Uri? RefreshUrl { get; set; }
	public Dictionary<string, string> Scopes { get; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	public OAuthFlow(Uri authorizationUrl, Uri tokenUrl, Dictionary<string, string> scopes)
	{
		AuthorizationUrl = authorizationUrl;
		TokenUrl = tokenUrl;
		Scopes = scopes;
	}

	public static OAuthFlow FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var flow = new OAuthFlow(
			obj.ExpectUri("authorizationUrl", "oauth flow"),
			obj.ExpectUri("tokenUrl", "oauth flow"),
			obj.ExpectMap("scopes", "oauth flow", x => x is JsonValue v && v.TryGetValue(out string? s) ? s : throw new JsonException("scopes must be strings")))
		{
			RefreshUrl = obj.MaybeUri("refreshUrl", "oauth flow"),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, flow.ExtensionData?.Keys);

		return flow;
	}

	public static JsonNode? ToNode(OAuthFlow? flow)
	{
		if (flow == null) return null;

		var obj = new JsonObject
		{
			["authorizationUrl"] = flow.AuthorizationUrl.ToString(),
			["tokenUrl"] = flow.TokenUrl.ToString()
		};

		var scopes = new JsonObject();
		foreach (var (key, value) in flow.Scopes)
		{
			scopes.Add(key, value);
		}
		obj.Add("scopes", scopes);

		obj.MaybeAdd("refreshUrl", flow.RefreshUrl?.ToString());
		obj.AddExtensions(flow.ExtensionData);

		return obj;
	}

	public object? Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		return ExtensionData?.Resolve(keys);
	}
}

internal class OAuthFlowJsonConverter : JsonConverter<OAuthFlow>
{
	public override OAuthFlow Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return OAuthFlow.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, OAuthFlow value, JsonSerializerOptions options)
	{
		var json = OAuthFlow.ToNode(value);

		JsonSerializer.Serialize(writer, json, options);
	}
}

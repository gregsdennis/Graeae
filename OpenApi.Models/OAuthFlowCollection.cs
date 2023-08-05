using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OpenApi.Models;

/// <summary>
/// Models the OAuth flow collection.
/// </summary>
[JsonConverter(typeof(OAuthFlowCollectionJsonConverter))]
public class OAuthFlowCollection : IRefTargetContainer
{
	private static readonly string[] KnownKeys =
	{
		"implicit",
		"password",
		"clientCredentials",
		"authorizationCode"
	};

	public OAuthFlow? Implicit { get; set; }
	public OAuthFlow? Password { get; set; }
	public OAuthFlow? ClientCredentials { get; set; }
	public OAuthFlow? AuthorizationCode { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	internal static OAuthFlowCollection FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var flows = new OAuthFlowCollection
		{
			Implicit = obj.Maybe("implicit", OAuthFlow.FromNode),
			Password = obj.Maybe("password", OAuthFlow.FromNode),
			ClientCredentials = obj.Maybe("clientCredentials", OAuthFlow.FromNode),
			AuthorizationCode = obj.Maybe("authorizationCode", OAuthFlow.FromNode),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, flows.ExtensionData?.Keys);

		return flows;
	}

	internal static JsonNode? ToNode(OAuthFlowCollection? flows)
	{
		if (flows == null) return null;

		var obj = new JsonObject();

		obj.MaybeAdd("implicit", OAuthFlow.ToNode(flows.Implicit));
		obj.MaybeAdd("password", OAuthFlow.ToNode(flows.Password));
		obj.MaybeAdd("clientCredentials", OAuthFlow.ToNode(flows.ClientCredentials));
		obj.MaybeAdd("authorizationCode", OAuthFlow.ToNode(flows.AuthorizationCode));
		obj.AddExtensions(flows.ExtensionData);

		return obj;
	}

	object? IRefTargetContainer.Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		int keysConsumed = 1;
		IRefTargetContainer? target = null;
		switch (keys[0])
		{
			case "implicit":
				target = Implicit;
				break;
			case "password":
				target = Password;
				break;
			case "clientCredentials":
				target = ClientCredentials;
				break;
			case "authorizationCode":
				target = AuthorizationCode;
				break;
		}

		return target != null
			? target.Resolve(keys[keysConsumed..])
			: ExtensionData?.Resolve(keys);
	}
}

internal class OAuthFlowCollectionJsonConverter : JsonConverter<OAuthFlowCollection>
{
	public override OAuthFlowCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return OAuthFlowCollection.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, OAuthFlowCollection value, JsonSerializerOptions options)
	{
		var json = OAuthFlowCollection.ToNode(value);

		JsonSerializer.Serialize(writer, json, options);
	}
}

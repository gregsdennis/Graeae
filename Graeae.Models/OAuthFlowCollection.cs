using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Graeae.Models;

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

	/// <summary>
	/// Gets or sets the implicit flow.
	/// </summary>
	public OAuthFlow? Implicit { get; set; }
	/// <summary>
	/// Gets or sets the password flow.
	/// </summary>
	public OAuthFlow? Password { get; set; }
	/// <summary>
	/// Gets or sets the client-credentials flow.
	/// </summary>
	public OAuthFlow? ClientCredentials { get; set; }
	/// <summary>
	/// Gets or sets the authorization-code flow.
	/// </summary>
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
        
        if (flows.Implicit is not null && flows.Implicit.AuthorizationUrl is null)
            throw new JsonException($"`authorizationUrl` is required for implicit oauth flow object");
        if (flows.Password is not null && flows.Password.TokenUrl is null)
            throw new JsonException($"`tokenUrl` is required for password oauth flow object");
        if (flows.ClientCredentials is not null && flows.ClientCredentials.TokenUrl is null)
            throw new JsonException($"`tokenUrl` is required for clientCredentials oauth flow object");
        if (flows.AuthorizationCode is not null)
        {
            if (flows.AuthorizationCode.AuthorizationUrl is null) throw new JsonException($"`authorizationUrl` is required for authorizationCode oauth flow object");
            if (flows.AuthorizationCode.TokenUrl is null) throw new JsonException($"`tokenUrl` is required for authorizationCode oauth flow object");
        }

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

	object? IRefTargetContainer.Resolve(ReadOnlySpan<string> keys)
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
			? target.Resolve(keys.Slice(keysConsumed))
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

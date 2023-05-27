using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenApi.Models;

public class OAuthFlowCollection
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
	public ExtensionData? ExtensionData { get; set; }

	public static OAuthFlowCollection FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var flows = new OAuthFlowCollection
		{
			Implicit = obj.Maybe("implicit", x => OAuthFlow.FromNode(x)),
			Password = obj.Maybe("password", x => OAuthFlow.FromNode(x)),
			ClientCredentials = obj.Maybe("clientCredentials", x => OAuthFlow.FromNode(x)),
			AuthorizationCode = obj.Maybe("authorizationCode", x => OAuthFlow.FromNode(x)),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, flows.ExtensionData?.Keys);

		return flows;
	}
}
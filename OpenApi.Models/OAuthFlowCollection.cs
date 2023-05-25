namespace OpenApi.Models;

public class OAuthFlowCollection
{
	public OAuthFlow? Implicit { get; set; }
	public OAuthFlow? Password { get; set; }
	public OAuthFlow? ClientCredentials { get; set; }
	public OAuthFlow? AuthorizationCode { get; set; }
	public ExtensionData? ExtensionData { get; set; }
}
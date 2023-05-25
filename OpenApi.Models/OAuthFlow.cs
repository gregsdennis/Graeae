namespace OpenApi.Models;

public class OAuthFlow
{
	public Uri AuthorizationUrl { get; }
	public Uri TokenUrl { get; }
	public Uri? RefreshUrl { get; set; }
	public Dictionary<string, string> Scopes { get; }
	public ExtensionData? ExtensionData { get; set; }

	public OAuthFlow(Uri authorizationUrl, Uri tokenUrl, Dictionary<string, string> scopes)
	{
		AuthorizationUrl = authorizationUrl ?? throw new ArgumentNullException(nameof(authorizationUrl));
		TokenUrl = tokenUrl ?? throw new ArgumentNullException(nameof(tokenUrl));
		Scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
	}
}
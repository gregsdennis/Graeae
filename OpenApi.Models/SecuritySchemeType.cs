namespace OpenApi.Models;

public enum SecuritySchemeType
{
	Unspecified,
	ApiKey,
	Http,
	MutualTls,
	Oauth2,
	OpenIdConnect
}
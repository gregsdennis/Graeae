﻿using Json.More;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace OpenApi.Models;

[JsonConverter(typeof(EnumStringConverter<SecuritySchemeType>))]
public enum SecuritySchemeType
{
	Unspecified,
	[Description("apiKey")]
	ApiKey,
	[Description("http")]
	Http,
	[Description("mutualTLS")]
	MutualTls,
	[Description("oauth2")]
	Oauth2,
	[Description("openIdConnect")]
	OpenIdConnect
}
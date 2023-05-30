using System.ComponentModel;
using System.Text.Json.Serialization;
using Json.More;

namespace OpenApi.Models;

[JsonConverter(typeof(EnumStringConverter<SecuritySchemeLocation>))]
public enum SecuritySchemeLocation
{
	Unspecified,
	[Description("query")]
	Query,
	[Description("query")]
	Header,
	[Description("query")]
	Cookie
}
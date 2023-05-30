using System.ComponentModel;
using System.Text.Json.Serialization;
using Json.More;

namespace OpenApi.Models;

[JsonConverter(typeof(EnumStringConverter<ParameterLocation>))]
public enum ParameterLocation
{
	Unspecified,
	[Description("query")]
	Query,
	[Description("header")]
	Header,
	[Description("path")]
	Path,
	[Description("cookie")]
	Cookie
}
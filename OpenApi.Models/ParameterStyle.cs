using System.ComponentModel;
using System.Text.Json.Serialization;
using Json.More;

namespace OpenApi.Models;

[JsonConverter(typeof(EnumStringConverter<ParameterStyle>))]
public enum ParameterStyle
{
	// see https://spec.openapis.org/oas/v3.1.0#style-values
	Unspecified,
	[Description("matrix")]
	Matrix,
	[Description("label")]
	Label,
	[Description("form")]
	Form,
	[Description("simple")]
	Simple,
	[Description("spaceDelimited")]
	SpaceDelimited,
	[Description("pipeDelimited")]
	PipeDelimited,
	[Description("deppObject")]
	DeepObject
}
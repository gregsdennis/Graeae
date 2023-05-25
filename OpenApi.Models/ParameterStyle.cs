namespace OpenApi.Models;

public enum ParameterStyle
{
	// see https://spec.openapis.org/oas/v3.1.0#style-values
	Unspecified,
	Matrix,
	Label,
	Form,
	Simple,
	SpaceDelimited,
	PipeDelimited,
	DeepObject
}
namespace OpenApi.Models;

/// <summary>
/// Defines the different types of runtime expressions.
/// </summary>
public enum RuntimeExpressionType
{
	Unspecified,
	Url,
	Method,
	StatusCode,
	Request,
	Response
}
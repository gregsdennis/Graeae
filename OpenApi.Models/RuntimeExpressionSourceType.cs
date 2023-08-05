namespace OpenApi.Models;

/// <summary>
/// Defines the different runtime expression sources.
/// </summary>
public enum RuntimeExpressionSourceType
{
	Unspecified,
	Header,
	Query,
	Path,
	Body
}
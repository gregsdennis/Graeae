namespace OpenApi.Models;

internal interface IRefTargetContainer
{
	object? Resolve(Span<string> keys);
}
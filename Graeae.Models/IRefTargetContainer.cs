namespace Graeae.Models;

internal interface IRefTargetContainer
{
	object? Resolve(Span<string> keys);
}
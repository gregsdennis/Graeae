namespace Graeae.Models;

internal interface IRefTargetContainer
{
	object? Resolve(ReadOnlySpan<string> keys);
}
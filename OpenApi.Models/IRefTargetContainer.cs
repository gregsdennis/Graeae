namespace OpenApi.Models;

public interface IRefTargetContainer
{
	public object? Resolve(Span<string> keys);
}
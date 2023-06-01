namespace OpenApi.Models;

public interface IRefResolvable
{
	public object? Resolve(Span<string> keys);
}
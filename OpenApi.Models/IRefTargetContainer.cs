namespace OpenApi.Models;

/// <summary>
/// Indicates that the implementation potentially contains `$ref` targets.
/// </summary>
public interface IRefTargetContainer
{
	/// <summary>
	/// Attempts to resolve a reference target.
	/// </summary>
	/// <param name="keys"></param>
	/// <returns></returns>
	public object? Resolve(Span<string> keys);
}
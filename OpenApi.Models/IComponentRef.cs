namespace OpenApi.Models;

/// <summary>
/// Indicates that the item is a reference to another object rather than being the object itself.
/// </summary>
public interface IComponentRef
{
	/// <summary>
	/// The URI for the reference.
	/// </summary>
	Uri Ref { get; }
	/// <summary>
	/// Gets the summary.
	/// </summary>
	string? Summary { get; }
	/// <summary>
	/// Gets the description.
	/// </summary>
	string? Description { get; }

	/// <summary>
	/// Resolves the reference.
	/// </summary>
	/// <param name="root">The document root.</param>
	/// <returns>A task.</returns>
	Task Resolve(OpenApiDocument root);
}
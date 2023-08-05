namespace OpenApi.Models;

/// <summary>
/// Indicates that the item is a reference to another object rather than being the object itself.
/// </summary>
internal interface IComponentRef
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
	/// Gets whether the reference has been resolved.
	/// </summary>
	bool IsResolved { get; }

	/// <summary>
	/// Resolves the reference.
	/// </summary>
	/// <param name="root">The document root.</param>
	/// <returns>A task.</returns>
	Task Resolve(OpenApiDocument root);
}
namespace Graeae.AspNet;

/// <summary>
/// Defines options for the <see cref="WebApplicationExtensions.MapOpenApi"/> extension.
/// </summary>
public class OpenApiOptions
{
	/// <summary>
	/// Provides a default set of options.
	/// </summary>
	public static OpenApiOptions Default { get; } = new();

	/// <summary>
	/// Ignores paths/routes that do not have handlers assigned.
	/// </summary>
	public bool IgnoreUnhandledPaths { get; set; }
}
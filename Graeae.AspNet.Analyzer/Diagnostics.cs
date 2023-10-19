using Microsoft.CodeAnalysis;

namespace Graeae.AspNet.Analyzer;

public static class Diagnostics
{
	public static Diagnostic NoPaths(string openApiFilePath) =>
		Diagnostic.Create(new("GR0001", "No paths", $"No paths are defined in '{openApiFilePath}'", "Path generation", DiagnosticSeverity.Info, true), Location.None, DiagnosticSeverity.Info);

	public static Diagnostic MissingRouteHandler(string route) =>
		Diagnostic.Create(new("GR0001", "Route not handled", $"Found no handler type for route '{route}'", "Path generation", DiagnosticSeverity.Warning, true), Location.None, DiagnosticSeverity.Warning);
}
using Microsoft.CodeAnalysis;

namespace Graeae.AspNet.Analyzer;

public static class Diagnostics
{
	public static Diagnostic OperationalError(string message) =>
		Diagnostic.Create(new("GR0001", "Operational error", message, "Operation", DiagnosticSeverity.Error, true), Location.None, DiagnosticSeverity.Error);

	public static Diagnostic NoPaths(string openApiFilePath) =>
		Diagnostic.Create(new("GR0002", "No paths", $"No paths are defined in '{openApiFilePath}'", "Path coverage", DiagnosticSeverity.Info, true), Location.None, DiagnosticSeverity.Info);

	public static Diagnostic MissingRouteHandler(string route) =>
		Diagnostic.Create(new("GR0003", "Route not handled", $"Found no handler type for route '{route}'", "Path coverage", DiagnosticSeverity.Warning, true), Location.None, DiagnosticSeverity.Warning);

	public static Diagnostic MissingRouteOperationHandler(string route, string op) =>
		Diagnostic.Create(new("GR0004", "Route not handled", $"Found no handler for '{op.ToUpperInvariant()} {route}'", "Path coverage", DiagnosticSeverity.Warning, true), Location.None, DiagnosticSeverity.Warning);
}
using Microsoft.CodeAnalysis;

namespace Graeae.AspNet.Analyzer;

internal static class Diagnostics
{
	public static Diagnostic OperationalError(string message) =>
		Diagnostic.Create(new("GR0001", "Operational error", message, "Operation", DiagnosticSeverity.Error, true), Location.None, DiagnosticSeverity.Error);

	public static Diagnostic NoPaths(string openApiFilePath) =>
		Diagnostic.Create(new("GR0002", "No paths", $"No paths are defined in '{openApiFilePath}'", "Path coverage", DiagnosticSeverity.Info, true), Location.None, DiagnosticSeverity.Info);

	public static Diagnostic MissingRouteHandler(string route) =>
		Diagnostic.Create(new("GR0003", "Route not handled", $"Found no handler type for route '{route}'", "Path coverage", DiagnosticSeverity.Warning, true), Location.None, DiagnosticSeverity.Warning);

	public static Diagnostic MissingRouteOperationHandler(string route, string op) =>
		Diagnostic.Create(new("GR0004", "Route not handled", $"Found no handler for '{op.ToUpperInvariant()} {route}'", "Path coverage", DiagnosticSeverity.Warning, true), Location.None, DiagnosticSeverity.Warning);

	public static Diagnostic AdditionalRouteHandler(string route) =>
		Diagnostic.Create(new("GR0005", "Route not published", $"Found handler type for route '{route}' but it does not appear in the OpenAPI definition", "Path coverage", DiagnosticSeverity.Warning, true), Location.None, DiagnosticSeverity.Warning);

	public static Diagnostic AdditionalRouteOperationHandler(string route, string op) =>
		Diagnostic.Create(new("GR0006", "Route not published", $"Found handler for '{op.ToUpperInvariant()} {route}' but it does not appear in the OpenAPI definition", "Path coverage", DiagnosticSeverity.Warning, true), Location.None, DiagnosticSeverity.Warning);

	public static Diagnostic ExternalFileAdded(string filePath) =>
		Diagnostic.Create(new("GR0007", "Document load success", $"File {filePath} added to document resolver", "OpenAPI docs", DiagnosticSeverity.Info, true), null, filePath);

	public static Diagnostic ExternalFileNotAdded(string filePath) =>
		Diagnostic.Create(new DiagnosticDescriptor("GR0008", "Document load failure", $"File {filePath} could not be added to document resolver", "OpenAPI docs", DiagnosticSeverity.Warning, true), null, filePath);
}
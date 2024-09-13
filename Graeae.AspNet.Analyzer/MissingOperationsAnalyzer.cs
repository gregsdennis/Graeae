using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Graeae.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Yaml2JsonNode;

namespace Graeae.AspNet.Analyzer;

/// <summary>
/// Outputs diagnostics when the OAI description defines routes and operations that aren't implemented.
/// </summary>
[Generator(LanguageNames.CSharp)]
internal class MissingOperationsAnalyzer : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var handlerClasses = context.SyntaxProvider.CreateSyntaxProvider(HandlerClassPredicate, HandlerClassTransform)
			.Where(x => x is not null);

		var files = context.AdditionalTextsProvider.Where(static file => file.Path.EndsWith("openapi.yaml"));
		var namesAndContents = files.Select((f, ct) => (Name: Path.GetFileNameWithoutExtension(f.Path), Content: f.GetText(ct)?.ToString(), Path: f.Path));

		context.RegisterSourceOutput(namesAndContents.Combine(handlerClasses.Collect()), AddDiagnostics);
	}

	private static bool HandlerClassPredicate(SyntaxNode node, CancellationToken token)
	{
		return node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };
	}

	private static (string, ClassDeclarationSyntax)? HandlerClassTransform(GeneratorSyntaxContext context, CancellationToken token)
	{
		var classDeclaration = (ClassDeclarationSyntax)context.Node;
		var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node);

		if (symbol is INamedTypeSymbol &&
		    classDeclaration.TryGetAttribute("Graeae.AspNet.RequestHandlerAttribute", context.SemanticModel, token, out var attribute) &&
			attribute!.TryGetStringParameter(out var route))
		{
			return (route!, classDeclaration);
		}

		return null;
	}

	private static void AddDiagnostics(SourceProductionContext context, ((string Name, string? Content, string Path) File, ImmutableArray<(string Route, ClassDeclarationSyntax Type)?> Handlers) source)
	{
		try
		{
			var file = source.File;
			if (file.Content == null)
				throw new Exception("Failed to read file \"" + file.Path + "\"");

			var doc = YamlSerializer.Deserialize<OpenApiDocument>(file.Content);
			doc!.Initialize().Wait();

			if (doc.Paths == null)
			{
				context.ReportDiagnostic(Diagnostics.NoPaths(file.Path));
				return;
			}

			foreach (var entry in doc.Paths)
			{
				var route = entry.Key.ToString();
				var handlerType = source.Handlers.FirstOrDefault(x => x?.Route == route)?.Type;

				if (handlerType is null)
				{
					context.ReportDiagnostic(Diagnostics.MissingRouteHandler(route));
					continue;
				}

				var methods = handlerType.Members.OfType<MethodDeclarationSyntax>().ToArray();

				if (!MethodExists(entry.Key, entry.Value.Get, nameof(PathItem.Get), methods))
					context.ReportDiagnostic(Diagnostics.MissingRouteOperationHandler(route, nameof(PathItem.Get)));
				if (!MethodExists(entry.Key, entry.Value.Post, nameof(PathItem.Post), methods))
					context.ReportDiagnostic(Diagnostics.MissingRouteOperationHandler(route, nameof(PathItem.Post)));
				if (!MethodExists(entry.Key, entry.Value.Put, nameof(PathItem.Put), methods))
					context.ReportDiagnostic(Diagnostics.MissingRouteOperationHandler(route, nameof(PathItem.Put)));
				if (!MethodExists(entry.Key, entry.Value.Delete, nameof(PathItem.Delete), methods))
					context.ReportDiagnostic(Diagnostics.MissingRouteOperationHandler(route, nameof(PathItem.Delete)));
				if (!MethodExists(entry.Key, entry.Value.Trace, nameof(PathItem.Trace), methods))
					context.ReportDiagnostic(Diagnostics.MissingRouteOperationHandler(route, nameof(PathItem.Trace)));
				if (!MethodExists(entry.Key, entry.Value.Options, nameof(PathItem.Options), methods))
					context.ReportDiagnostic(Diagnostics.MissingRouteOperationHandler(route, nameof(PathItem.Options)));
				if (!MethodExists(entry.Key, entry.Value.Head, nameof(PathItem.Head), methods))
					context.ReportDiagnostic(Diagnostics.MissingRouteOperationHandler(route, nameof(PathItem.Head)));
			}
		}
		catch (Exception e)
		{
			var errorMessage = $"Error: {e.Message}\n\nStack trace: {e.StackTrace}\n\nStack trace: {e.InnerException?.StackTrace}";
			context.ReportDiagnostic(Diagnostics.OperationalError(errorMessage));
		}
	}

	private static bool MethodExists(PathTemplate route, Operation? op, string opName, IEnumerable<MethodDeclarationSyntax> methods)
	{
		if (op is null) return true;

		// parameters can be implicitly or explicitly bound
		//
		// - path
		//   - implicitly bound by name
		//   - explicitly bound with [FromRoute(Name = "name")]
		// - query
		//   - implicitly bound by name
		//   - explicitly bound with [FromQuery(Name = "name")]
		// - header
		//   - explicitly bound with [FromHeader(Name = "name")]
		// - body
		//   - implicitly bound by model

		var implicitOpenApiParameters = route.Segments.Select(x =>
		{
			var match = PathHelpers.TemplatedSegmentPattern.Match(x);
			return match.Success
				? new Parameter(match.Groups["param"].Value, ParameterLocation.Path)
				: null;
		}).Where(x => x is not null);
		if (op.RequestBody is not null) 
			implicitOpenApiParameters = implicitOpenApiParameters.Append(Parameter.Body);
		var explicitOpenapiParameters = op.Parameters?.Select(x => new Parameter(x.Name, x.In)) ?? [];
		var openApiParameters = implicitOpenApiParameters.Union(explicitOpenapiParameters).ToArray();
		var methodParameterLists = methods.Where(x => string.Equals(x.Identifier.ValueText, opName, StringComparison.InvariantCultureIgnoreCase))
			.Select(x => x.ParameterList.Parameters.SelectMany(AnalysisExtensions.GetParameters));

		return methodParameterLists.Any(methodParameterList => openApiParameters.All(methodParameterList.Contains));
	}
}
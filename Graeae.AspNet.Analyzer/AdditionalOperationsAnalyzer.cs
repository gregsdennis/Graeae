using System;
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
/// Outputs diagnostics for handlers that handle routes or operations that are not listed in the OAI description.
/// </summary>
[Generator(LanguageNames.CSharp)]
internal class AdditionalOperationsAnalyzer : IIncrementalGenerator
{
	private static OpenApiDocument[]? OpenApiDocs { get; set; }

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		OpenApiDocs = null;

		var handlerClasses = context.SyntaxProvider.CreateSyntaxProvider(HandlerClassPredicate, HandlerClassTransform)
			.Where(x => x is not null);

		var files = context.AdditionalTextsProvider.Where(static file => file.Path.EndsWith("openapi.yaml"));
		var namesAndContents = files.Select((f, ct) => (Name: Path.GetFileNameWithoutExtension(f.Path), Content: f.GetText(ct)?.ToString(), Path: f.Path));

		context.RegisterSourceOutput(handlerClasses.Combine(namesAndContents.Collect()), AddDiagnostics);
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

	private static void AddDiagnostics(SourceProductionContext context, ((string Route, ClassDeclarationSyntax Type)? Handler, ImmutableArray<(string Name, string? Content, string Path)> Files) source)
	{
		try
		{
			OpenApiDocs ??= source.Files.Select(file =>
			{
				if (file.Content == null)
					throw new Exception("Failed to read file \"" + file.Path + "\"");
				var doc = YamlSerializer.Deserialize<OpenApiDocument>(file.Content);
				doc!.Initialize().Wait();

				return doc;
			}).ToArray();

			var handler = source.Handler!.Value;

			var allPaths = OpenApiDocs.SelectMany(x => x.Paths).ToList();
			var path = allPaths.FirstOrDefault(x => x.Key.ToString() == handler.Route);
			if (path.Key is null)
			{
				context.ReportDiagnostic(Diagnostics.AdditionalRouteHandler(handler.Route));
				return;
			}

			var route = path.Key;
			var pathItem = path.Value;

			var methods = handler.Type.Members.OfType<MethodDeclarationSyntax>().ToArray();

			foreach (var method in methods)
			{
				var (op, name) = GetMatchingOperation(method, pathItem);
				if (!OperationExists(route, op, method))
					context.ReportDiagnostic(Diagnostics.AdditionalRouteOperationHandler(route.ToString(), name!));
			}
		}
		catch (Exception e)
		{
			//Debug.Break();
			var errorMessage = $"Error: {e.Message}\n\nStack trace: {e.StackTrace}\n\nStack trace: {e.InnerException?.StackTrace}";
			context.ReportDiagnostic(Diagnostics.OperationalError(errorMessage));
		}
	}

	private static (Operation? Op, string? Name) GetMatchingOperation(MethodDeclarationSyntax method, PathItem pathItem) =>
		method.Identifier.ValueText.ToUpperInvariant() switch
		{
			"GET" => (pathItem.Get, nameof(pathItem.Get)),
			"POST" => (pathItem.Post, nameof(pathItem.Post)),
			"PUT" => (pathItem.Put, nameof(pathItem.Put)),
			"DELETE" => (pathItem.Delete, nameof(pathItem.Delete)),
			"TRACE" => (pathItem.Trace, nameof(pathItem.Trace)),
			"OPTIONS" => (pathItem.Options, nameof(pathItem.Options)),
			"HEAD" => (pathItem.Head, nameof(pathItem.Head)),
			_ => (null, null)
		};

	private static bool OperationExists(PathTemplate route, Operation? op, MethodDeclarationSyntax method)
	{
		if (op is null) return false;

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
		var methodParameterList = method.ParameterList.Parameters.SelectMany(AnalysisExtensions.GetParameters);

		return openApiParameters.All(x => methodParameterList.Contains(x));
	}
}
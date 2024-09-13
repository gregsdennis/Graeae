using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Graeae.Models;
using Json.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Yaml2JsonNode;

namespace Graeae.AspNet.Analyzer;

/// <summary>
/// Outputs diagnostics for handlers that handle routes or operations that are not listed in the OAI description.
/// </summary>
[Generator(LanguageNames.CSharp)]
internal class AdditionalOperationsAnalyzer : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
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
			TryGetAttribute(classDeclaration, "Graeae.AspNet.RequestHandlerAttribute", context.SemanticModel, token, out var attribute) &&
			TryGetStringParameter(attribute!, out var route))
		{
			return (route!, classDeclaration);
		}

		return null;
	}

	private static bool TryGetAttribute(ClassDeclarationSyntax candidate, string attributeName, SemanticModel semanticModel, CancellationToken cancellationToken, out AttributeSyntax? value)
	{
		foreach (var attributeList in candidate.AttributeLists)
		{
			foreach (var attribute in attributeList.Attributes)
			{
				var info = semanticModel.GetSymbolInfo(attribute, cancellationToken);
				var symbol = info.Symbol;

				if (symbol is IMethodSymbol method
				    && method.ContainingType.ToDisplayString().Equals(attributeName, StringComparison.Ordinal))
				{
					value = attribute;
					return true;
				}
			}
		}

		value = null;
		return false;
	}

	private static bool TryGetStringParameter(AttributeSyntax attribute, out string? value)
	{
		if (attribute.ArgumentList is
		    {
			    Arguments.Count: 1,
		    } argumentList)
		{
			var argument = argumentList.Arguments[0];

			if (argument.Expression is LiteralExpressionSyntax literal)
			{
				value = literal.Token.Value?.ToString();
				return true;
			}
		}

		value = null;
		return false;
	}

	private static void AddDiagnostics(SourceProductionContext context, ((string Route, ClassDeclarationSyntax Type)? Handler, ImmutableArray<(string Name, string? Content, string Path)> Files) source)
	{
		try
		{
			// TODO: cache this somehow; don't want to do this for every handler
			var docs = source.Files.Select(file =>
			{
				if (file.Content == null)
					throw new Exception("Failed to read file \"" + file.Path + "\"");
				var doc = YamlSerializer.Deserialize<OpenApiDocument>(file.Content);
				doc!.Initialize().Wait();

				return doc;
			}).ToArray();

			var handler = source.Handler!.Value;

			var allPaths = docs.SelectMany(x => x.Paths).ToList();
			var path = allPaths.FirstOrDefault(x => x.Key.ToString() == handler.Route);
			if (path.Key is null)
			{
				context.ReportDiagnostic(Diagnostics.AdditionalRouteHandler(handler.Route));
				return;
			}

			var route = path.Key;
			var pathItem = path.Value;

			// need to invert this loop and check the collection of paths against each method

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

	private static readonly Regex TemplatedSegmentPattern = new(@"^\{(?<param>.*)\}$", RegexOptions.Compiled | RegexOptions.ECMAScript);

	private record Parameter
	{
		public static readonly Parameter Body = new(string.Empty, ParameterLocation.Unspecified);

		public string Name { get; }
		public ParameterLocation In { get; }

		public Parameter(string name, ParameterLocation @in)
		{
			Name = @in == ParameterLocation.Header ? name.ToLowerInvariant() : name;
			In = @in;
		}
	}

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
			var match = TemplatedSegmentPattern.Match(x);
			return match.Success
				? new Parameter(match.Groups["param"].Value, ParameterLocation.Path)
				: null;
		}).Where(x => x is not null);
		if (op.RequestBody is not null) 
			implicitOpenApiParameters = implicitOpenApiParameters.Append(Parameter.Body);
		var explicitOpenapiParameters = op.Parameters?.Select(x => new Parameter(x.Name, x.In)) ?? [];
		var openApiParameters = implicitOpenApiParameters.Union(explicitOpenapiParameters).ToArray();
		var methodParameterList = method.ParameterList.Parameters.SelectMany(GetParameters);

		return openApiParameters.All(methodParameterList.Contains);
	}

	private static IEnumerable<Parameter> GetParameters(ParameterSyntax parameter)
	{
		if (TryGetAttribute(parameter.AttributeLists, "FromRoute", out var attribute) &&
		    TryGetStringParameter(attribute!, out var name))
			yield return new Parameter(name!, ParameterLocation.Path);
		else if (TryGetAttribute(parameter.AttributeLists, "FromQuery", out attribute) &&
		         TryGetStringParameter(attribute!, out name))
			yield return new Parameter(name!, ParameterLocation.Query);
		else if (TryGetAttribute(parameter.AttributeLists, "FromHeader", out attribute) &&
		         TryGetStringParameter(attribute!, out name))
			yield return new Parameter(name!, ParameterLocation.Header);
		else if (TryGetAttribute(parameter.AttributeLists, "FromBody", out _))
			yield return Parameter.Body;
		else if (TryGetAttribute(parameter.AttributeLists, "FromServices", out _))
		{
		}
		else
		{
			// if no attributes are found then consider all implicit options
			yield return new Parameter(parameter.Identifier.ValueText, ParameterLocation.Path);
			yield return new Parameter(parameter.Identifier.ValueText, ParameterLocation.Query);
			// TODO: this is catching services and the http context
			//yield return Parameter.Body;
		}
	}

	private static bool TryGetAttribute(SyntaxList<AttributeListSyntax> attributeLists, string attributeName, out AttributeSyntax? attribute)
	{
		foreach (var attributeList in attributeLists)
		{
			foreach (var att in attributeList.Attributes)
			{
				if (att.Name.ToString() == attributeName)
				{
					attribute = att;
					return true;
				}
			}
		}

		attribute = null;
		return false;
	}
}

internal static class Debug
{
	[Conditional("DEBUG")]
	public static void Break()
	{
		if (!Debugger.IsAttached) Debugger.Launch(); else Debugger.Break();
	}
}
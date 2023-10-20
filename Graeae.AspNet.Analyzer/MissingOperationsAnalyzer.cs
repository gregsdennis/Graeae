using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using Graeae.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
			.Where(x => x is not null)
			.Collect();

		var files = context.AdditionalTextsProvider.Where(static file => file.Path.EndsWith("openapi.yaml"));
		var namesAndContents = files.Select((f, ct) => (Name: Path.GetFileNameWithoutExtension(f.Path), Content: f.GetText(ct)?.ToString(), Path: f.Path));

		context.RegisterSourceOutput(namesAndContents.Combine(handlerClasses), AddDiagnostics);
	}

	private static bool HandlerClassPredicate(SyntaxNode node, CancellationToken token)
	{
		return node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };
	}

	private static (string, ClassDeclarationSyntax)? HandlerClassTransform(GeneratorSyntaxContext context, CancellationToken token)
	{
		var classDeclaration = Unsafe.As<ClassDeclarationSyntax>(context.Node);
		var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node);

		if (symbol is INamedTypeSymbol type &&
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

				if (!CheckOperation(entry.Key, entry.Value.Get, nameof(PathItem.Get), methods))
					context.ReportDiagnostic(Diagnostics.MissingRouteOperationHandler(route, nameof(PathItem.Get)));
				if (!CheckOperation(entry.Key, entry.Value.Post, nameof(PathItem.Post), methods))
					context.ReportDiagnostic(Diagnostics.MissingRouteOperationHandler(route, nameof(PathItem.Post)));
				if (!CheckOperation(entry.Key, entry.Value.Put, nameof(PathItem.Put), methods))
					context.ReportDiagnostic(Diagnostics.MissingRouteOperationHandler(route, nameof(PathItem.Put)));
				if (!CheckOperation(entry.Key, entry.Value.Delete, nameof(PathItem.Delete), methods))
					context.ReportDiagnostic(Diagnostics.MissingRouteOperationHandler(route, nameof(PathItem.Delete)));
				if (!CheckOperation(entry.Key, entry.Value.Trace, nameof(PathItem.Trace), methods))
					context.ReportDiagnostic(Diagnostics.MissingRouteOperationHandler(route, nameof(PathItem.Trace)));
				if (!CheckOperation(entry.Key, entry.Value.Options, nameof(PathItem.Head), methods))
					context.ReportDiagnostic(Diagnostics.MissingRouteOperationHandler(route, nameof(PathItem.Options)));
				if (!CheckOperation(entry.Key, entry.Value.Head, nameof(PathItem.Head), methods))
					context.ReportDiagnostic(Diagnostics.MissingRouteOperationHandler(route, nameof(PathItem.Head)));
			}
		}
		catch (Exception e)
		{
			var errorMessage = $"Error: {e.Message}\n\nStack trace: {e.StackTrace}";
			context.ReportDiagnostic(Diagnostics.OperationalError(errorMessage));
		}
	}

	private static readonly Regex TemplatedSegmentPattern = new(@"^\{(?<param>.*)\}$", RegexOptions.Compiled | RegexOptions.ECMAScript);

	private record Parameter
	{
		public string Name { get; }
		public ParameterLocation In { get; }

		public Parameter(string name, ParameterLocation @in)
		{
			Name = name;
			In = @in;
		}
	}

	private static bool CheckOperation(PathTemplate route, Operation? op, string opName, IEnumerable<MethodDeclarationSyntax> methods)
	{
		if (op is null) return true;

		// TODO: body parameters
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
			if (match.Success)
				return new Parameter(match.Groups["param"].Value, ParameterLocation.Path);

			return null;
		}).Where(x => x is not null);
		var explicitOpenapiParameters = op.Parameters?.Select(x => new Parameter(x.Name, x.In)) ?? Enumerable.Empty<Parameter>();
		var openApiParameters = implicitOpenApiParameters.Union(explicitOpenapiParameters).ToArray();
		var methodParameterLists = methods.Where(x => string.Equals(x.Identifier.ValueText, opName, StringComparison.InvariantCultureIgnoreCase))
			.Select(x => x.ParameterList.Parameters.SelectMany(GetParameters));

		return methodParameterLists.Any(methodParameterList => openApiParameters.All(methodParameterList.Contains));
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
		else
		{
			// if no attributes are found then consider both implicit options
			yield return new Parameter(parameter.Identifier.ValueText, ParameterLocation.Path);
			yield return new Parameter(parameter.Identifier.ValueText, ParameterLocation.Query);
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
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

	private static (string, INamedTypeSymbol)? HandlerClassTransform(GeneratorSyntaxContext context, CancellationToken token)
	{
		var classDeclaration = Unsafe.As<ClassDeclarationSyntax>(context.Node);
		var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node);

		if (symbol is INamedTypeSymbol type &&
		    TryGetAttribute(classDeclaration, "Graeae.AspNet.RequestHandlerAttribute", context.SemanticModel, token, out var attribute) &&
		    TryGetStringParameter(attribute!, out var route))
		{
			return (route!, type);
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

	private static void AddDiagnostics(SourceProductionContext context, ((string Name, string? Content, string Path) File, ImmutableArray<(string Route, INamedTypeSymbol Type)?> Handlers) source)
	{
		var file = source.File;

		try
		{
			if (file.Content == null)
				throw new Exception("Failed to read file \"" + file.Path + "\"");

			var doc = YamlSerializer.Deserialize<OpenApiDocument>(file.Content);

			if (doc!.Paths == null)
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

				// TODO: search handler type for methods
			}
		}
		catch (Exception e)
		{
			var errorMessage = $"Error: {e.Message}\n\nStack trace: {e.StackTrace}";
		}
	}
}
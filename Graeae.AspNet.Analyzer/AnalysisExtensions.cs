using System;
using System.Collections.Generic;
using System.Threading;
using Graeae.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Graeae.AspNet.Analyzer;

internal static class AnalysisExtensions
{
	public static bool TryGetAttribute(this ClassDeclarationSyntax candidate, string attributeName, SemanticModel semanticModel, CancellationToken cancellationToken, out AttributeSyntax? value)
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

	public static bool TryGetStringParameter(this AttributeSyntax attribute, out string? value)
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

	public static IEnumerable<Parameter> GetParameters(this ParameterSyntax parameter)
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

	/// <summary>
	/// determine the namespace the class/enum/struct is declared in, if any
	/// </summary>
	public static string GetNamespace(this BaseTypeDeclarationSyntax syntax)
	{
		// If we don't have a namespace at all we'll return an empty string
		// This accounts for the "default namespace" case
		string nameSpace = string.Empty;

		// Get the containing syntax node for the type declaration
		// (could be a nested type, for example)
		SyntaxNode? potentialNamespaceParent = syntax.Parent;

		// Keep moving "out" of nested classes etc until we get to a namespace
		// or until we run out of parents
		while (potentialNamespaceParent != null &&
		       potentialNamespaceParent is not NamespaceDeclarationSyntax
		       && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
		{
			potentialNamespaceParent = potentialNamespaceParent.Parent;
		}

		// Build up the final namespace by looping until we no longer have a namespace declaration
		if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
		{
			// We have a namespace. Use that as the type
			nameSpace = namespaceParent.Name.ToString();

			// Keep moving "out" of the namespace declarations until we 
			// run out of nested namespace declarations
			while (true)
			{
				if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
				{
					break;
				}

				// Add the outer namespace as a prefix to the final namespace
				nameSpace = $"{namespaceParent.Name}.{nameSpace}";
				namespaceParent = parent;
			}
		}

		// return the final namespace
		return nameSpace;
	}
}
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
}

internal static class PathHelpers
{
	public static readonly Regex TemplatedSegmentPattern = new(@"^\{(?<param>.*)\}$", RegexOptions.Compiled | RegexOptions.ECMAScript);
}
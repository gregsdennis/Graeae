using System;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Graeae.AspNet.Analyzer;

[Generator(LanguageNames.CSharp)]
public class Analyzer : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var triggerClasses = context.SyntaxProvider.CreateSyntaxProvider(HandlerClassPredicate, HandlerClassTransform)
			.Where(x => x is not null);
		context.RegisterSourceOutput(triggerClasses.Collect(), CacheNamespace);
	}

	private static bool HandlerClassPredicate(SyntaxNode node, CancellationToken token)
	{
		return node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };
	}

	private static ClassDeclarationSyntax HandlerClassTransform(GeneratorSyntaxContext context, CancellationToken token)
	{
		var classDeclaration = (ClassDeclarationSyntax)context.Node;
		// actually check for the attribute...
		return classDeclaration;
	}

	private static void CacheNamespace(SourceProductionContext context, ImmutableArray<ClassDeclarationSyntax> classDeclarationSyntaxes)
	{
		var content = """
		              namespace GeneratedCode;

		              public class Janitor
		              {
		                  public string Name { get; set; } = null!;
		                  public int Age { get; set; }
		              }
		              """;

		//if (!Debugger.IsAttached) Debugger.Launch();
		//else Debugger.Break();

		try
		{
			context.AddSource("TestCode.cs", SourceText.From(content, Encoding.UTF8));
		}
		catch (Exception e)
		{
			context.ReportDiagnostic(Diagnostic.Create(new("GR0001", "Operational error", e.Message, "Operation", DiagnosticSeverity.Error, true), Location.None, DiagnosticSeverity.Error));
		}
	}

}
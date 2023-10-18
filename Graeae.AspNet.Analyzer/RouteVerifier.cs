using System;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Graeae.AspNet.Analyzer;

[Generator(LanguageNames.CSharp)]
public class RouteVerifier : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var files = context.AdditionalTextsProvider.Where(static file => file.Path.EndsWith("openapi.yaml"));
		var namesAndContents = files.Select((file, cancellationToken) => (Name: Path.GetFileNameWithoutExtension(file.Path), Content: file.GetText(cancellationToken)?.ToString(), Path: file.Path));
		context.RegisterSourceOutput(namesAndContents, AddSource);
	}

	private static void AddSource(SourceProductionContext context, (string Name, string? Content, string Path) file)
	{
		var fileName = $"{file.Name}.g.cs";

		try
		{
			if (file.Content == null)
				throw new Exception("Failed to read file \"" + file.Path + "\"");

			//string sourceCode = .....


			//context.AddSource(fileName, sourceCode);
		}
		catch (Exception e)
		{
			var errorMessage = $"Error: {e.Message}\n\nStack trace: {e.StackTrace}";

			//context.AddSource(fileName, sourceCode);
		}
	}
}
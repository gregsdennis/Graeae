using System;
using System.Collections.Generic;
using System.IO;
using Graeae.Models;
using Json.Schema;
using Microsoft.CodeAnalysis;
using Yaml2JsonNode;

namespace Graeae.AspNet.Analyzer;

[Generator(LanguageNames.CSharp)]
internal class ModelGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var files = context.AdditionalTextsProvider.Where(static file => file.Path.EndsWith("openapi.yaml"));
		var namesAndContents = files.Select((f, ct) => (Name: Path.GetFileNameWithoutExtension(f.Path), Content: f.GetText(ct)?.ToString(), Path: f.Path));

		context.RegisterSourceOutput(namesAndContents, AddSource);
	}

	private void AddSource(SourceProductionContext context, (string Name, string? Content, string Path) file)
	{
		try
		{
			if (file.Content == null)
				throw new Exception("Failed to read file \"" + file.Path + "\"");

			var doc = YamlSerializer.Deserialize<OpenApiDocument>(file.Content);
			doc!.Initialize().Wait();

			// TODO: find other schemas
			var schemasToGenerate = doc.Components?.Schemas ?? new Dictionary<string, JsonSchema>();

			foreach (var entry in schemasToGenerate)
			{
				GenerateCodeForSchema(context, entry.Key, entry.Value);
			}
		}
		catch (Exception e)
		{
			var errorMessage = $"Error: {e.Message}\n\nStack trace: {e.StackTrace}";
			context.ReportDiagnostic(Diagnostics.OperationalError(errorMessage));
		}
	}

	private void GenerateCodeForSchema(SourceProductionContext context, string name, JsonSchema schema)
	{
		throw new NotImplementedException();
	}
}
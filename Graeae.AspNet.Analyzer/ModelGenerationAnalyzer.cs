using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using Corvus.Json;
using Corvus.Json.CodeGeneration;
using Corvus.Json.CodeGeneration.CSharp;
using Graeae.Models;
using Json.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Yaml2JsonNode;
using Encoding = System.Text.Encoding;
using VocabularyRegistry = Corvus.Json.CodeGeneration.VocabularyRegistry;

namespace Graeae.AspNet.Analyzer;

[Generator(LanguageNames.CSharp)]
internal class ModelGenerationAnalyzer : IIncrementalGenerator
{
	private static string _namespace = null!;

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// need to identify the namespace to generate models in
		var handlerClasses = context.SyntaxProvider.CreateSyntaxProvider(HandlerClassPredicate, HandlerClassTransform)
			.Where(x => x is not null);
		context.RegisterSourceOutput(handlerClasses.Collect(), CacheNamespace);

		// generate the models
		var files = context.AdditionalTextsProvider.Where(static file => file.Path.EndsWith("openapi.yaml"));
		var namesAndContents = files.Select((f, ct) => (Name: Path.GetFileNameWithoutExtension(f.Path), Content: f.GetText(ct)?.ToString(), Path: f.Path));
		context.RegisterSourceOutput(namesAndContents.Collect(), AddDiagnostics);
	}

	private static bool HandlerClassPredicate(SyntaxNode node, CancellationToken token)
	{
		return node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };
	}

	private static string? HandlerClassTransform(GeneratorSyntaxContext context, CancellationToken token)
	{
		var classDeclaration = (ClassDeclarationSyntax)context.Node;
		return classDeclaration.GetNamespace();
	}

	private static void CacheNamespace(SourceProductionContext context, ImmutableArray<string?> namespaces)
	{
		_namespace = (namespaces.Distinct().FirstOrDefault() ?? "Graeae.AspNet") + ".Models";
	}

	private static void AddDiagnostics(SourceProductionContext context, ImmutableArray<(string Name, string? Content, string Path)> files)
	{
		try
		{
			var documentResolver = new CompoundDocumentResolver(new FileSystemDocumentResolver(), new HttpClientDocumentResolver(new HttpClient()));
			var references = new List<JsonReference>();

			foreach (var file in files)
			{
				if (file.Content == null)
					throw new Exception("Failed to read file \"" + file.Path + "\"");

				var yaml = YamlSerializer.Parse(file.Content);
				var node = yaml.ToJsonNode().FirstOrDefault();
				if (node is null) continue;

				var doc = JsonDocument.Parse(node.ToString());
				var added = documentResolver.AddDocument(file.Path, doc);

				var openapiDoc = node.Deserialize<OpenApiDocument>()!;
				references.AddRange(openapiDoc.FindSchemaLocations(file.Path));

				context.ReportDiagnostic(added ? Diagnostics.ExternalFileAdded(file.Path) : Diagnostics.ExternalFileNotAdded(file.Path));
			}

			RegisterMetaSchemas(documentResolver);
			var typeBuilder = new JsonSchemaTypeBuilder(documentResolver, RegisterVocabularies(documentResolver));
			var typeDeclarations = references.Select(r => typeBuilder.AddTypeDeclarations(r, Corvus.Json.CodeGeneration.Draft202012.VocabularyAnalyser.DefaultVocabulary));
			var languageProvider = CSharpLanguageProvider.DefaultWithOptions(new CSharpLanguageProvider.Options(_namespace));
			var generatedCode = typeBuilder.GenerateCodeUsing(languageProvider, CancellationToken.None, typeDeclarations);

			foreach (var codeFile in generatedCode)
			{
				context.AddSource(codeFile.FileName, SourceText.From(codeFile.FileContent, Encoding.UTF8));
			}
		}
		catch (Exception e)
		{
			Debug.Inject();
			var errorMessage = $"Error: {e.Message}\n\nStack trace: {e.StackTrace}\n\nStack trace: {e.InnerException?.StackTrace}";
			context.ReportDiagnostic(Diagnostics.OperationalError(errorMessage));
		}
	}

	private static void RegisterMetaSchemas(IDocumentResolver documentResolver)
	{
		void RegisterMetaSchema(string uri, JsonSchema schema)
		{
			var doc = JsonSerializer.SerializeToDocument(schema);
			documentResolver.AddDocument(uri, doc);
		}

		RegisterMetaSchema(MetaSchemas.Draft6Id.ToString(), MetaSchemas.Draft6);
		RegisterMetaSchema(MetaSchemas.Draft7Id.ToString(), MetaSchemas.Draft7);
		RegisterMetaSchema(MetaSchemas.Draft201909Id.ToString(), MetaSchemas.Draft201909);
		RegisterMetaSchema(MetaSchemas.Draft202012Id.ToString(), MetaSchemas.Draft202012);
		RegisterMetaSchema(Json.Schema.OpenApi.MetaSchemas.OpenApiMetaId.ToString(), Json.Schema.OpenApi.MetaSchemas.OpenApiMeta);

	}

	private static VocabularyRegistry RegisterVocabularies(IDocumentResolver documentResolver)
	{
		VocabularyRegistry vocabularyRegistry = new();

		// Add support for the vocabularies we are interested in.
		Corvus.Json.CodeGeneration.Draft6.VocabularyAnalyser.RegisterAnalyser(vocabularyRegistry);
		Corvus.Json.CodeGeneration.Draft7.VocabularyAnalyser.RegisterAnalyser(vocabularyRegistry);
		Corvus.Json.CodeGeneration.Draft201909.VocabularyAnalyser.RegisterAnalyser(documentResolver, vocabularyRegistry);
		Corvus.Json.CodeGeneration.Draft202012.VocabularyAnalyser.RegisterAnalyser(documentResolver, vocabularyRegistry);
		Corvus.Json.CodeGeneration.OpenApi30.VocabularyAnalyser.RegisterAnalyser(vocabularyRegistry);

		// And register the custom vocabulary for Corvus extensions.
		vocabularyRegistry.RegisterVocabularies(Corvus.Json.CodeGeneration.CorvusVocabulary.SchemaVocabulary.DefaultInstance);
		return vocabularyRegistry;
	}
}
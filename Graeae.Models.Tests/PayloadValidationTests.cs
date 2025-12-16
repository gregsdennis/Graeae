using System.Text.Json;
using Json.Pointer;
using Json.Schema;
using Dialect = Graeae.Models.SchemaDraft4.Dialect;

namespace Graeae.Models.Tests;

public class PayloadValidationTests
{

	[TestCase("payload-valid.json")]
	public async Task ReferencesValid(string fileName)
	{
		var schemaFileName = GetFile("schema-components.json");
		var fileText = await File.ReadAllTextAsync(schemaFileName);
		var fileJson = JsonDocument.Parse(fileText).RootElement;

		var options = new BuildOptions
		{
			Dialect = Dialect.Draft4,
			SchemaRegistry = new()
		};
        var openApiDoc = OpenApiDocument.Build(fileJson, options);

		var componentRef = JsonPointer.Parse("#/components/schemas/outer");
		var fullFileName = GetFile(fileName);
		var payloadJson = await File.ReadAllTextAsync(fullFileName);
		var document = JsonDocument.Parse(payloadJson).RootElement;

		var results = openApiDoc.EvaluatePayload(document, componentRef);
		Assert.That(results!.IsValid, Is.True);
	}


	[TestCase("payload-invalid1.json")]
	[TestCase("payload-invalid2.json")]
	[TestCase("payload-invalid3.json")]
	[TestCase("payload-invalid4.json")]
	public async Task ReferencesInvalid(string fileName)
	{
		var schemaFileName = GetFile("schema-components.json");

		var fileText = await File.ReadAllTextAsync(schemaFileName);
        var fileJson = JsonDocument.Parse(fileText).RootElement;

        var options = new BuildOptions
        {
            Dialect = Dialect.Draft4,
            SchemaRegistry = new()
        };
        var openApiDoc = OpenApiDocument.Build(fileJson, options);

        var componentRef = JsonPointer.Parse("#/components/schemas/outer");

		var fullFileName = GetFile(fileName);
		var payloadJson = await File.ReadAllTextAsync(fullFileName);
		var document = JsonDocument.Parse(payloadJson).RootElement;

		var results = openApiDoc.EvaluatePayload(document, componentRef);
		Assert.That(results!.IsValid, Is.False);
	}
}
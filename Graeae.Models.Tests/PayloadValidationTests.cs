using System.Text.Json;
using Graeae.Models.SchemaDraft4;
using Json.Pointer;
using Json.Schema;

namespace Graeae.Models.Tests;

public class PayloadValidationTests
{

	[TestCase("payload-valid.json")]
	public async Task ReferencesValid(string fileName)
	{
		var schemaFileName = GetFile("schema-components.json");
		//IBaseDocument schema = JsonSchema.FromFile(schemaFileName);
		//SchemaRegistry.Global.Register(schema);
		var fileText = File.ReadAllText(schemaFileName);
		var openApiDoc = JsonSerializer.Deserialize(fileText, TestSerializerContext.Default.OpenApiDocument);

		var options = new EvaluationOptions
		{
			EvaluateAs = Draft4Support.Draft4Version,
		};
		await openApiDoc!.Initialize(options.SchemaRegistry);

		var componentRef = JsonPointer.Parse("#/components/schemas/outer");
		var schema = openApiDoc.Find<JsonSchema>(componentRef);

		var fullFileName = GetFile(fileName);
		var payloadJson = File.ReadAllText(fullFileName);
		var document = JsonDocument.Parse(payloadJson);
		//var options = new EvaluationOptions
		//{
		//	EvaluateAs = Draft4Support.Draft4Version,
		//};

		//JsonSchema validateSchema = new JsonSchemaBuilder()
		//	.Ref(new Uri(schema.BaseUri, componentRef));

		var results = schema!.Evaluate(document, options);
		Assert.True(results.IsValid);
	}


	[TestCase("payload-invalid1.json")]
	[TestCase("payload-invalid2.json")]
	[TestCase("payload-invalid3.json")]
	[TestCase("payload-invalid4.json")]
	public async Task ReferencesInvalid(string fileName)
	{
		var schemaFileName = GetFile("schema-components.json");
		//IBaseDocument schema = JsonSchema.FromFile(schemaFileName);
		//SchemaRegistry.Global.Register(schema);

		var fileText = File.ReadAllText(schemaFileName);
		var openApiDoc = JsonSerializer.Deserialize(fileText, TestSerializerContext.Default.OpenApiDocument);

		var options = new EvaluationOptions
		{
			EvaluateAs = Draft4Support.Draft4Version,
		};
		await openApiDoc!.Initialize(options.SchemaRegistry);

		var componentRef = JsonPointer.Parse("#/components/schemas/outer");
		var schema = openApiDoc.Find<JsonSchema>(componentRef);

		var fullFileName = GetFile(fileName);
		var payloadJson = File.ReadAllText(fullFileName);
		var document = JsonDocument.Parse(payloadJson);
		//var options = new EvaluationOptions
		//{
		//	EvaluateAs = Draft4Support.Draft4Version,
		//};

		var results = schema.Evaluate(document, options);
		Assert.False(results.IsValid);
	}
}
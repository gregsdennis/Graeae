using System.Text.Json;
using System.Text.Json.Nodes;
using Graeae.Models.SchemaDraft4;
using Json.Pointer;
using Json.Schema;
using Yaml2JsonNode;

namespace Graeae.Models.Tests;

public class ValidationTests
{
	[Test]
	[TestCase("api-with-examples.yaml")]
	[TestCase("callback-example.yaml")]
	[TestCase("link-example.yaml")]
	[TestCase("petstore.yaml")]
	[TestCase("petstore-expanded.yaml")]
	[TestCase("uspto.yaml")]
	[TestCase("postman.yaml")]
	public void ValidateOpenApiDoc_3_0(string fileName)
	{
		var fullFileName = GetFile(fileName);
		var yaml = File.ReadAllText(fullFileName);
		var instance = YamlSerializer.Parse(yaml).First().ToJsonNode();
		var schemaFileName = GetFile("openapi-schema-3.0.json");
		var schema = JsonSchema.FromFile(schemaFileName, TestEnvironment.SerializerOptions);

		var results = schema.Evaluate(instance, new EvaluationOptions
		{
			OutputFormat = OutputFormat.List,
			EvaluateAs = SchemaDraft4.Draft4Support.Draft4Version
		});

		Console.WriteLine(JsonSerializer.Serialize(results, TestEnvironment.TestOutputSerializerOptions));

		Assert.IsTrue(results.IsValid);
	}

	[Test]
	[TestCase("non-oauth-scopes.yaml")]
	[TestCase("webhook-example.yaml")]
	public void ValidateOpenApiDoc_3_1(string fileName)
	{
		var fullFileName = GetFile(fileName);
		var yaml = File.ReadAllText(fullFileName);
		var instance = YamlSerializer.Parse(yaml).First().ToJsonNode();
		var schemaFileName = GetFile("openapi-schema-3.1.json");
		var schema = JsonSchema.FromFile(schemaFileName, TestEnvironment.SerializerOptions);

		var results = schema.Evaluate(instance, new EvaluationOptions
		{
			OutputFormat = OutputFormat.List
		});

		Console.WriteLine(JsonSerializer.Serialize(results, TestEnvironment.TestOutputSerializerOptions));

		Assert.IsTrue(results.IsValid);
	}

	[Test]
	[TestCase("payload-valid.json")]
	public void ReferencesValid(string fileName)
	{
		var schemaFileName = GetFile("schema-components.json");
		IBaseDocument schema = JsonSchema.FromFile(schemaFileName);
		SchemaRegistry.Global.Register(schema);

		var componentRef = "#/components/schemas/outer";

		var fullFileName = GetFile(fileName);
		var payloadJson = File.ReadAllText(fullFileName);
		var document = JsonDocument.Parse(payloadJson);
		var options = new EvaluationOptions
		{
			EvaluateAs = Draft4Support.Draft4Version,
		};

		JsonSchema validateSchema = new JsonSchemaBuilder()
			.Ref(new Uri(schema.BaseUri, componentRef));

		var results = validateSchema.Evaluate(document, options);
		Assert.True(results.IsValid);
	}


	[Test]
	[TestCase("payload-invalid1.json")]
	[TestCase("payload-invalid2.json")]
	[TestCase("payload-invalid3.json")]
	[TestCase("payload-invalid4.json")]
	public void ReferencesInvalid(string fileName)
	{
		var schemaFileName = GetFile("schema-components.json");
		IBaseDocument schema = JsonSchema.FromFile(schemaFileName);
		SchemaRegistry.Global.Register(schema);

		var componentRef = "#/components/schemas/outer";

		var fullFileName = GetFile(fileName);
		var payloadJson = File.ReadAllText(fullFileName);
		var document = JsonDocument.Parse(payloadJson);
		var options = new EvaluationOptions
		{
			EvaluateAs = Draft4Support.Draft4Version,
		};

		JsonSchema validateSchema = new JsonSchemaBuilder()
			.Ref(new Uri(schema.BaseUri, componentRef));

		var results = validateSchema.Evaluate(document, options);
		Assert.False(results.IsValid);
	}
}

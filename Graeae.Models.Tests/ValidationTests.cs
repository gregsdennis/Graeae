using System.Text.Encodings.Web;
using System.Text.Json;
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
		var schema = JsonSchema.FromFile(schemaFileName);

		var results = schema.Evaluate(instance, new EvaluationOptions
		{
			OutputFormat = OutputFormat.List,
			EvaluateAs = SchemaDraft4.Draft4Support.Draft4Version
		});

		Console.WriteLine(JsonSerializer.Serialize(results, new JsonSerializerOptions
		{
			WriteIndented = true,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		}));

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
		var schema = JsonSchema.FromFile(schemaFileName);

		var results = schema.Evaluate(instance, new EvaluationOptions
		{
			OutputFormat = OutputFormat.List
		});

		Console.WriteLine(JsonSerializer.Serialize(results, new JsonSerializerOptions
		{
			WriteIndented = true,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		}));

		Assert.IsTrue(results.IsValid);
	}
}

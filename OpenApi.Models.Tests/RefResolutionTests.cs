using System.Net;
using System.Text.Json.Nodes;
using Json.More;
using Json.Pointer;
using Json.Schema;

namespace OpenApi.Models.Tests;

public class RefResolutionTests
{
	[Test]
	public void ResolvePathItem()
	{
		var file = "api-with-examples.yaml";
		var fullFileName = GetFile(file);

		var document = YamlSerializer.Deserialize<OpenApiDocument>(File.ReadAllText(fullFileName));

		var pathItem = document!.Find<PathItem>(JsonPointer.Parse("/paths/~1v2"));

		Assert.That(pathItem!.Get!.OperationId, Is.EqualTo("getVersionDetailsv2"));
	}

	[Test]
	public void ResolveExample()
	{
		var file = "api-with-examples.yaml";
		var fullFileName = GetFile(file);

		var document = YamlSerializer.Deserialize<OpenApiDocument>(File.ReadAllText(fullFileName));

		var example = document!.Find<Example>(JsonPointer.Parse("/paths/~1v2/get/responses/203/content/application~1json/examples/foo"));

		Assert.That(example!.Value!["version"]!["updated"]!.GetValue<string>(), Is.EqualTo("2011-01-21T11:33:21Z"));
	}

	[Test]
	public void SchemaRefResolvesToAnotherPartOfOpenApiDoc()
	{
		var document = new OpenApiDocument
		{
			Components = new ComponentCollection
			{
				Schemas = new Dictionary<string, JsonSchema>
				{
					["start"] = new JsonSchemaBuilder()
						.Ref("#/components/schemas/target"),
					["target"] = new JsonSchemaBuilder()
						.Type(SchemaValueType.Object)
						.Properties(
							("foo", new JsonSchemaBuilder().Type(SchemaValueType.String))
						)
				}
			}
		};

		var options = new EvaluationOptions();
		document.Initialize(options.SchemaRegistry);

		var start = document.Find<JsonSchema>(JsonPointer.Parse("/components/schemas/start"));

		var instance = new JsonObject { ["foo"] = "a string" };

		var validation = start!.Evaluate(instance, options);

		Assert.IsTrue(validation.IsValid);
	}

	[Test]
	public void ExampleRefIsResolved()
	{
		var document = new OpenApiDocument
		{
			Paths = new()
			{
				["/v2"] = new()
				{
					Get = new()
					{
						Responses = new()
						{
							[HttpStatusCode.OK] = new()
							{
								Content = new()
								{
									["application/json"] = new()
									{
										Examples = new()
										{
											["foo"] = new ExampleRef("#/components/examples/foo")
										}
									}
								}
							}
						}
					}
				}
			},
			Components = new()
			{
				Examples = new()
				{
					["foo"] = new()
					{
						Value = 42
					}
				}
			}
		};

		var options = new EvaluationOptions();
		document.Initialize(options.SchemaRegistry);

		var inlineExample = document.Find<Example>(JsonPointer.Parse("/paths/~1v2/get/responses/200/content/application~1json/examples/foo"));

		Assert.That(inlineExample!.Value!.AsValue().GetNumber(), Is.EqualTo(42));
	}
}
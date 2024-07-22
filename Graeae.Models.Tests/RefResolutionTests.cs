using System.Net;
using System.Text.Json.Nodes;
using Json.More;
using Json.Pointer;
using Json.Schema;
using Yaml2JsonNode;

namespace Graeae.Models.Tests;

public class RefResolutionTests
{
	[Test]
	public void ResolvePathItem()
	{
		var file = "api-with-examples.yaml";
		var fullFileName = GetFile(file);

		var document = YamlSerializer.Deserialize<OpenApiDocument>(File.ReadAllText(fullFileName), TestEnvironment.SerializerOptions);

		var pathItem = document!.Find<PathItem>(JsonPointer.Parse("/paths/~1v2"));

		Assert.That(pathItem!.Get!.OperationId, Is.EqualTo("getVersionDetailsv2"));
	}

	[Test]
	public void ResolveExample()
	{
		var file = "api-with-examples.yaml";
		var fullFileName = GetFile(file);

		var document = YamlSerializer.Deserialize<OpenApiDocument>(File.ReadAllText(fullFileName), TestEnvironment.SerializerOptions);

		var example = document!.Find<Example>(JsonPointer.Parse("/paths/~1v2/get/responses/203/content/application~1json/examples/foo"));

		Assert.That(example!.Value!["version"]!["updated"]!.GetValue<string>(), Is.EqualTo("2011-01-21T11:33:21Z"));
	}

	[Test]
	public async Task SchemaRefResolvesToAnotherPartOfOpenApiDoc()
	{
		var document = new OpenApiDocument("3.1.0", new OpenApiInfo("title", "v1"))
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
		await document.Initialize(options.SchemaRegistry);

		var start = document.Find<JsonSchema>(JsonPointer.Parse("/components/schemas/start"));

		var instance = new JsonObject { ["foo"] = "a string" };

		var validation = start!.Evaluate(instance, options);

		Assert.That(validation.IsValid, Is.True);
	}

	[Test]
	public async Task ExampleRefIsResolved()
	{
		var document = new OpenApiDocument("3.1.0", new OpenApiInfo("title", "v1"))
		{
			Paths = new()
			{
				["/v2"] = new()
				{
					Get = new()
					{
						Responses = new()
						{
							[HttpStatusCode.OK] = new("description")
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
		await document.Initialize(options.SchemaRegistry);

		var inlineExample = document.Find<Example>(JsonPointer.Parse("/paths/~1v2/get/responses/200/content/application~1json/examples/foo"));

		Assert.That(inlineExample!.Value!.AsValue().GetNumber(), Is.EqualTo(42));
	}

	[Test]
	public async Task RefFetchedFromFile()
	{
		try
		{
			Ref.Fetch = async uri =>
			{
				var fileName = uri.OriginalString.Replace("http://localhost:1234/", string.Empty);
				var fullFileName = GetFile(fileName);

				var content = await File.ReadAllTextAsync(fullFileName);

				return JsonNode.Parse(content);
			};

			var document = new OpenApiDocument("3.1.0", new OpenApiInfo("title", "v1"))
			{
				Paths = new()
				{
					["/v2"] = new()
					{
						Get = new()
						{
							Responses = new()
							{
								[HttpStatusCode.OK] = new("description")
								{
									Content = new()
									{
										["application/json"] = new()
										{
											Examples = new()
											{
												["foo"] = new ExampleRef("http://localhost:1234/ref-target.json#/foo/example")
											}
										}
									}
								}
							}
						}
					}
				}
			};

			var options = new EvaluationOptions();
			await document.Initialize(options.SchemaRegistry);

			var reffedExample = document.Find<Example>(JsonPointer.Parse("/paths/~1v2/get/responses/200/content/application~1json/examples/foo"));

			var expected = new JsonObject
			{
				["type"] = "string"
			};

			Assert.That(() => reffedExample!.Value.IsEquivalentTo(expected));
		}
		finally
		{
			Ref.Fetch = Ref.FetchJson;
		}
	}
}
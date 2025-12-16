using System.Net;
using System.Text.Json;
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

		Assert.That(example!.Value?.GetProperty("version").GetProperty("updated").GetString(), Is.EqualTo("2011-01-21T11:33:21Z"));
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

		var options = new BuildOptions
        {
			Dialect = Dialect.Draft202012,
			SchemaRegistry = new()
        };
		document.Initialize(options);

		var start = document.Find<JsonSchema>(JsonPointer.Parse("/components/schemas/start"));

		var instance = JsonDocument.Parse("""{ "foo": "a string" }""").RootElement;

		var validation = start!.Evaluate(instance);

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
						Value = 42.AsJsonElement()
					}
				}
			}
		};

        var options = new BuildOptions
        {
			Dialect = Dialect.Draft202012,
            SchemaRegistry = new()
        };
		document.Initialize(options);

		var inlineExample = document.Find<Example>(JsonPointer.Parse("/paths/~1v2/get/responses/200/content/application~1json/examples/foo"));

		Assert.That(inlineExample?.Value?.GetInt32(), Is.EqualTo(42));
	}

	[Test]
	public async Task RefFetchedFromFile()
	{
		try
		{
			Ref.Fetch = uri =>
			{
				var fileName = uri.OriginalString.Replace("http://localhost:1234/", string.Empty);
				var fullFileName = GetFile(fileName);

				var content = File.ReadAllText(fullFileName);

				return JsonDocument.Parse(content).RootElement;
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

			var options = new BuildOptions
            {
				Dialect = Dialect.Draft202012,
				SchemaRegistry = new()
            };
			document.Initialize(options);

			var reffedExample = document.Find<Example>(JsonPointer.Parse("/paths/~1v2/get/responses/200/content/application~1json/examples/foo"));

            var expected = JsonDocument.Parse("""{ "type": "string" }""").RootElement;

			Assert.That(reffedExample!.Value?.IsEquivalentTo(expected), Is.True);
		}
		finally
		{
			Ref.Fetch = Ref.FetchJson;
		}
	}
}
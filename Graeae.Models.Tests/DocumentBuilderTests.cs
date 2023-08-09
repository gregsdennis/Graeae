using System.Net;
using System.Text.Json.Nodes;
using Json.Schema;
using Yaml2JsonNode;

namespace Graeae.Models.Tests;

// These are more of language support tests than functional tests.
// The idea here is verifying that building a document in
// code is simple.
public class DocumentBuilderTests
{
	public void BuildDocument()
	{
		var doc = new OpenApiDocument("3.1.0", new("title", "1.0")
		{
			Contact = new()
			{
				Email = "me@you.com",
				Name = "me you",
				Url = new Uri("https://you.com"),
				ExtensionData = new() { ["key"] = new JsonArray { 1, 2, 3 } }
			},
			Description = "this is an api",
			License = new("generic license")
			{
				Identifier = "GEN1.0",
				Url = new Uri("https://genlicense.info"),
				ExtensionData = new() { ["key"] = 42 }
			},
			Summary = "summary",
			TermsOfService = new Uri("https://you.com/terms")
		})
		{
			Paths = new()
			{
				["/test/{item}"] = new()
				{
					Get = new()
					{
						Callbacks = new()
						{
							["basic"] = Ref.To.Callback("basic")
						},
						Parameters = new[] { Ref.To.Parameter("item") }
					},
					Post = new()
					{
						Deprecated = true,
						Description = "don't use this",
						ExternalDocs = new("http://you.com/docs")
						{
							Description = "docs"
						},
						OperationId = "operation",
						Parameters = new[] { Ref.To.Parameter("item") },
						RequestBody = new(new()
						{
							["application/json"] = new()
							{
								Encoding = new()
								{
									["base64"] = new()
									{
										AllowReserved = true,
										ContentType = "text/plain",
										Explode = false,
										Style = ParameterStyle.Simple
									}
								}
							}
						}),
						Responses = new()
						{
							[HttpStatusCode.OK] = Ref.To.Response("item")
						} 
					}
				}
			},
			Components = new()
			{
				Callbacks = new()
				{
					["basic"] = new()
					{
						["{$request.query.callbackUrl}"] = new()
						{
							Post = new()
							{
								RequestBody = new(new()
								{
									["application"] = new()
									{
										Schema = new JsonSchemaBuilder()
											.Type(SchemaValueType.Object)
											.PropertyNames(new JsonSchemaBuilder().Pattern(@"^[1-9][0-9]*$"))
											.AdditionalProperties(new JsonSchemaBuilder().Type(SchemaValueType.Boolean))
									}
								}),
								ExtensionData = new()
								{
									["x-comment"] = "this is a numerically-index dictionary of booleans"
								}
							}
						}
					}
				},
				Parameters = new()
				{
					["item"] = new("item content", ParameterLocation.Path)
					{
						AllowEmptyValue = false,
						AllowReserved = true,
						Required = true
					}
				},
				Responses = new()
				{
					["item"] = new("item response")
					{
						Content = new()
						{
							["application/json"] = new()
							{
								Schema = new JsonSchemaBuilder()
									.Type(SchemaValueType.Object)
									.Properties(
										("name", new JsonSchemaBuilder().Type(SchemaValueType.String))
									),
								Example = new JsonObject { ["name"] = "example item" }
							}
						}
					}
				}
			}
		};
	}

	public void PetStoreExample()
	{
		var document = new OpenApiDocument("3.0.0",
			new("Swagger Petstore", "1.0.0")
			{
				License = new("MIT")
			}
		)
		{
			Servers = new[]
			{
				new Server("http://petstore.swagger.io/v1")
			},
			Paths = new()
			{
				["/pets"] = new()
				{
					Get = new()
					{
						Summary = "List all pets",
						OperationId = "listPets",
						Tags = new[] { "pets" },
						Parameters = new[]
						{
							new Parameter("limit", ParameterLocation.Query)
							{
								Description = "How many items to return at one time (max 100)",
								Required = false,
								Schema = new JsonSchemaBuilder()
									.Type(SchemaValueType.Integer)
									.Maximum(100)
									.Format(Formats.Int32)
							}
						},
						Responses = new()
						{
							[HttpStatusCode.OK] = new("A paged array of pets")
							{
								Headers = new()
								{
									["x-next"] = new()
									{
										Description = "A link to the next page of responses",
										Schema = new JsonSchemaBuilder().Type(SchemaValueType.String)
									}
								},
								Content = new()
								{
									["application/json"] = new()
									{
										Schema = Ref.To.Schema("Pets")
									}
								}
							},
							Default = new("unexpected error")
							{
								Content = new()
								{
									["application/json"] = new()
									{
										Schema = Ref.To.Schema("Error")
									}
								}
							}
						}
					},
					Post = new()
					{
						Summary = "Create a pet",
						OperationId = "createPets",
						Tags = new[] { "pets" },
						Responses = new()
						{
							[HttpStatusCode.Created] = new("Null response"),
							Default = new("unexpected error")
							{
								Content = new()
								{
									["application/json"] = new()
									{
										Schema = Ref.To.Schema("Error")
									}
								}
							}
						}
					}
				},
				["/pets/{petId}"] = new()
				{
					Get = new()
					{
						Summary = "Info for a specific pet",
						OperationId = "showPetById",
						Tags = new[] { "pets" },
						Parameters = new[]
						{
							new Parameter("petId", ParameterLocation.Path)
							{
								Required = true,
								Description = "The id of the pet to retrieve",
								Schema = new JsonSchemaBuilder()
									.Type(SchemaValueType.String)
							}
						},
						Responses = new()
						{
							[HttpStatusCode.OK] = new("Expected response to a valid request")
							{
								Content = new()
								{
									["application/json"] = new()
									{
										Schema = Ref.To.Schema("Pet")
									}
								}
							},
							Default = new("unexpected error")
							{
								Content = new()
								{
									["application/json"] = new()
									{
										Schema = Ref.To.Schema("Error")
									}
								}
							}
						}
					}
				}
			},
			Components = new()
			{
				Schemas = new()
				{
					["Pet"] = new JsonSchemaBuilder()
						.Type(SchemaValueType.Object)
						.Required("id", "name")
						.Properties(
							("id", new JsonSchemaBuilder()
								.Type(SchemaValueType.Integer)
								.Format(Formats.Int64)
							),
							("name", new JsonSchemaBuilder().Type(SchemaValueType.String)),
							("tag", new JsonSchemaBuilder().Type(SchemaValueType.String))
						),
					["Pets"] = new JsonSchemaBuilder()
						.Type(SchemaValueType.Array)
						.MaxItems(100)
						.Items(Ref.To.Schema("Pet")),
					["Error"] = new JsonSchemaBuilder()
						.Type(SchemaValueType.Object)
						.Required("code", "message")
						.Properties(
							("code", new JsonSchemaBuilder()
								.Type(SchemaValueType.Integer)
								.Format(Formats.Int32)
							),
							("message", new JsonSchemaBuilder().Type(SchemaValueType.String))
						)
				}
			}
		};

		Console.WriteLine(YamlSerializer.Serialize(document));
	}
}
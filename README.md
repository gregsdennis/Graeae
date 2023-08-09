[![Build & Test](https://github.com/gregsdennis/openapi/actions/workflows/dotnet-core.yml/badge.svg?branch=main&event=push)](https://github.com/gregsdennis/openapi/actions/workflows/dotnet-core.yml)
[![Percentage of issues still open](http://isitmaintained.com/badge/open/gregsdennis/openapi.svg)](http://isitmaintained.com/project/gregsdennis/openapi "Percentage of issues still open")
[![Average time to resolve an issue](http://isitmaintained.com/badge/resolution/gregsdennis/openapi.svg)](http://isitmaintained.com/project/gregsdennis/openapi "Average time to resolve an issue")
[![License](https://img.shields.io/github/license/gregsdennis/openapi)](https://github.com/gregsdennis/openapi/blob/main/LICENSE)
<!-- [![Test results](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/gregsdennis/28607f2d276032f4d9a7f2c807e44df7/raw/test-results-badge.json)](https://github.com/gregsdennis/json-everything/actions?query=workflow%3A%22Build+%26+Test%22) -->

# STJ.OpenApi.Models

[![](https://img.shields.io/nuget/vpre/STJ.OpenApi.Models.svg?svg=true) ![](https://img.shields.io/nuget/dt/STJ.OpenApi.Models.svg?svg=true)](https://www.nuget.org/packages/STJ.OpenAPI.Models)

OpenAPI models for System.Text.Json.  Supports specification versions v3.0.x & v3.1.

This project is supported by the [`json-everything`](https://github.com/gregsdennis/json-everything) project:

- JSON Schema support provided by [JsonSchema.Net](https://www.nuget.org/packages/JsonSchema.Net)
- YAML support provided by [Yaml2JsonNode](https://www.nuget.org/packages/Yaml2JsonNode)

## Usage

The library supports OpenAPI v3.1 (de)serialization out of the box.

```c#
// read from a file
var yamlText = File.ReadAllText("openapi.yaml");
var openApiDoc = YamlSerializer.Deserialize<OpenApiDocument>(yamlText);

// verify and resolve references
openApiDoc.Initialize();

// back to text
var asText = YamlSerializer.Serialize(openApiDoc);
```

***HINT** Because YAML is a superset of JSON, the `YamlSerializer` class also supports JSON files, so you don't need to check which format the file is in.*

During initialization, if the document contains references that cannot be resolved, a `RefResolutionException` will be thrown.

### OpenAPI 3.0.x and JSON Schema Draft 4

To support OpenAPI v3.0.x, you'll need to enable JSON Schema draft 4 support first.  To do that, add this to your app initialization:

```c#
using OpenApi.Models.SchemaDraft4;

Draft4Support.Enable();
```

When building schemas, you may find that the default extensions for _JsonSchema.Net_'s `JsonSchemaBuilder` are insufficient since it only supports draft 6 and after out of the box.  For example, in draft 4, `exclusiveMinimum` takes a boolean, but in draft 6 and after, it needs to be a number.

To support these differences, additional extension methods have been added to support specific draft 4 and OpenAPI functionality.  They're available for when they're needed, but otherwise the extensions that come with _JsonSchema.Net_ will work.

| Extension | Function |
|:--|:--|
| `.OasId(Uri)`<br>`.OasId(string)` | Adds the `id` (no `$`) keyword |
| `.OasType(SchemaValueType)` | Adds a `type` keyword variant that supports the OAS notion |
| `.Nullable(bool)` | Adds the `nullable` keyword |
| `.ExclusiveMaximum(bool)` | Adds a boolean-valued `exclusiveMaximum` keyword |
| `.ExclusiveMinimum(bool)` | Adds a boolean-valued `exclusiveMinimum` keyword |

### External reference resolution

The `OpenApiDocument.Initialize()` method will scan the document model and attempt to resolve any references.  References to locations within the document are automatically supported, however references to external locations are not supported by default.

To enable external reference resolution, you'll need to set the `Ref.Fetch` function property.  This static property is a function which takes a single `Uri` argument and returns an `Task<JsonNode?>` which will then be deserialized into the appropriate model.

A (very) basic implementation that supports `http(s):` URIs is provided as the `Ref.FetchJson()` method.  It also supports YAML content.  It's likely you'll want to provide your own method for production scenarios, but this will get you started.

```c#
Ref.Fetch = Ref.FetchJson;
```

### Creating OpenAPI documents inline

The models are read/write to make it simple to define an OpenAPI document in code.

<details>
<summary>Expand to see a code sample</summary>

(from https://github.com/OAI/OpenAPI-Specification/blob/main/examples/v3.0/petstore.yaml)

YAML:
```yaml
openapi: "3.0.0"
info:
  version: 1.0.0
  title: Swagger Petstore
  license:
    name: MIT
servers:
  - url: http://petstore.swagger.io/v1
paths:
  /pets:
    get:
      summary: List all pets
      operationId: listPets
      tags:
        - pets
      parameters:
        - name: limit
          in: query
          description: How many items to return at one time (max 100)
          required: false
          schema:
            type: integer
            maximum: 100
            format: int32
      responses:
        '200':
          description: A paged array of pets
          headers:
            x-next:
              description: A link to the next page of responses
              schema:
                type: string
          content:
            application/json:    
              schema:
                $ref: "#/components/schemas/Pets"
        default:
          description: unexpected error
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/Error"
    post:
      summary: Create a pet
      operationId: createPets
      tags:
        - pets
      responses:
        '201':
          description: Null response
        default:
          description: unexpected error
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/Error"
  /pets/{petId}:
    get:
      summary: Info for a specific pet
      operationId: showPetById
      tags:
        - pets
      parameters:
        - name: petId
          in: path
          required: true
          description: The id of the pet to retrieve
          schema:
            type: string
      responses:
        '200':
          description: Expected response to a valid request
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/Pet"
        default:
          description: unexpected error
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/Error"
components:
  schemas:
    Pet:
      type: object
      required:
        - id
        - name
      properties:
        id:
          type: integer
          format: int64
        name:
          type: string
        tag:
          type: string
    Pets:
      type: array
      maxItems: 100
      items:
        $ref: "#/components/schemas/Pet"
    Error:
      type: object
      required:
        - code
        - message
      properties:
        code:
          type: integer
          format: int32
        message:
          type: string
```

Equivalent C#:

```c#
var document = new OpenApiDocument("3.0.0",
	new("Swagger Petstore", "1.0.0")
	{
		License = new("MIT")
	}
)
{
	Servers = new []
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
				Tags = new []{"pets"},
				Parameters = new []
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
							["x-next"] = new ()
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
				Tags = new []{"pets"},
				Responses = new()
				{
					[HttpStatusCode.Created] = new("Null response"),
					Default = new("unexpected error")
					{
						Content = new(){
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
				Tags = new []{"pets"},
				Parameters = new []
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
```

</details>


There are several more examples in the test project.

## Contributing and Support

This project is in its infancy and is open for help and suggestions.  Additional functionality such as code generation is planned as extension libraries.

Feel free to open issues & pull requests.

Remember to follow the [Code of Conduct](./CODE_OF_CONDUCT.md) and [Contributing Guidelines](./CONTRIBUTING.md).

To chat about this project, please [join me in Slack](https://join.slack.com/t/manateeopensource/shared_invite/enQtMzU4MjgzMjgyNzU3LWZjYzAzYzY3NjY1MjY3ODI0ZGJiZjc3Nzk1MDM5NTNlMjMyOTE0MzMxYWVjMjdiOGU1NDY5OGVhMGQ5YzY4Zjg).

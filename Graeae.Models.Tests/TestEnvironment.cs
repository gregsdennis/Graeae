using Graeae.Models.SchemaDraft4;
using Json.Schema;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Graeae.Models.Tests;

[SetUpFixture]
public class TestEnvironment
{
	public static readonly JsonSerializerOptions SerializerOptions =
		new()
		{
			TypeInfoResolverChain = { TestSerializerContext.Default },
		};

	public static readonly JsonSerializerOptions TestOutputSerializerOptions =
		new()
		{
			TypeInfoResolverChain = { TestSerializerContext.Default },
			WriteIndented = true,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		};

	[OneTimeSetUp]
	public void Setup()
    {
        var options = new BuildOptions
        {
            Dialect = SchemaDraft4.Dialect.Draft4,
            SchemaRegistry = new()
        };

		MetaSchema.Register(options);
	}
}

[JsonSerializable(typeof(OpenApiDocument))]
[JsonSerializable(typeof(ParameterStyle))]
[JsonSerializable(typeof(ParameterStyle?))]
[JsonSerializable(typeof(ParameterLocation))]
[JsonSerializable(typeof(ParameterLocation?))]
[JsonSerializable(typeof(SecuritySchemeLocation))]
[JsonSerializable(typeof(SecuritySchemeLocation?))]

[JsonSerializable(typeof(JsonSchema))]
[JsonSerializable(typeof(EvaluationResults))]

[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(JsonNode))]
[JsonSerializable(typeof(JsonObject))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, JsonSchema>))]
internal partial class TestSerializerContext : JsonSerializerContext;
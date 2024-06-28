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
		Draft4Support.Enable();
	}
}

[JsonSerializable(typeof(OpenApiDocument))]
[JsonSerializable(typeof(Draft4ExclusiveMaximumKeyword))]
[JsonSerializable(typeof(Draft4ExclusiveMinimumKeyword))]
[JsonSerializable(typeof(Draft4IdKeyword))]
[JsonSerializable(typeof(Draft4TypeKeyword))]
[JsonSerializable(typeof(NullableKeyword))]
[JsonSerializable(typeof(ParameterStyle))]
[JsonSerializable(typeof(ParameterStyle?))]
[JsonSerializable(typeof(ParameterLocation))]
[JsonSerializable(typeof(ParameterLocation?))]
[JsonSerializable(typeof(SecuritySchemeLocation))]
[JsonSerializable(typeof(SecuritySchemeLocation?))]

[JsonSerializable(typeof(JsonSchema))]
[JsonSerializable(typeof(EvaluationResults))]

[JsonSerializable(typeof(JsonNode))]
[JsonSerializable(typeof(JsonObject))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, JsonSchema>))]
internal partial class TestSerializerContext : JsonSerializerContext;
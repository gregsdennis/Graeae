using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Graeae.Models.SchemaDraft4;
using Json.Schema;

namespace Graeae.Models;

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
[JsonSerializable(typeof(SchemaValueType))]

[JsonSerializable(typeof(JsonNode))]
[JsonSerializable(typeof(JsonObject))]
internal partial class GraeaeSerializerContext : JsonSerializerContext;
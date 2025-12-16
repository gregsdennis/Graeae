using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Graeae.Models.SchemaDraft4;
using Json.Schema;

namespace Graeae.Models;

[JsonSerializable(typeof(OpenApiDocument))]
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

[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(JsonNode))]
[JsonSerializable(typeof(JsonObject))]
internal partial class GraeaeSerializerContext : JsonSerializerContext;
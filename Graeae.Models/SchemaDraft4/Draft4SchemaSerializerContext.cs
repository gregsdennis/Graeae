using System.Text.Json.Serialization;
using Json.Schema;

namespace Graeae.Models.SchemaDraft4;

[JsonSerializable(typeof(Draft4ExclusiveMaximumKeyword))]
[JsonSerializable(typeof(Draft4ExclusiveMinimumKeyword))]
[JsonSerializable(typeof(Draft4IdKeyword))]
[JsonSerializable(typeof(Draft4TypeKeyword))]
[JsonSerializable(typeof(NullableKeyword))]
[JsonSerializable(typeof(SchemaValueType))]
internal partial class Draft4SchemaSerializerContext : JsonSerializerContext;

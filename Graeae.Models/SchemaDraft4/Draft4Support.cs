using System.Text.Json.Nodes;
using Json.Schema;

namespace Graeae.Models.SchemaDraft4;

/// <summary>
/// Provides additional functionality for JSON Schema draft 4 support.
/// </summary>
public static class Draft4Support
{
	/// <summary>
	/// Defines a JSON Schema draft 4 spec version.
	/// </summary>
	// This is kind of a hack since SpecVersion is an enum.
	// Maybe it should be defined as string constants.
	// This assumes that JsonSchema.Net supports custom spec versions (hidden feature).
	public const SpecVersion Draft4Version = (SpecVersion)(1 << 10);
	/// <summary>
	/// Defines the OpenAPI / JSON Schema draft 4 `file` type.
	/// </summary>
	public const SchemaValueType FileDataType = (SchemaValueType)(1 << 10);

	/// <summary>
	/// Defines the JSON Schema draft 4 meta-schema URI.
	/// </summary>
	public const string Draft4MetaSchemaUri = "http://json-schema.org/draft-04/schema#";

	/// <summary>
	/// Defines the JSON Schema draft 4 meta-schema.
	/// </summary>
	/// <remarks>
	/// This property is initialized by the <see cref="Enable"/> method and
	/// will be null if accessed before that method is called.
	/// </remarks>
	public static JsonSchema Draft4MetaSchema { get; private set; } = null!;

	/// <summary>
	/// Enables support for OpenAPI v3.0 and JSON Schema draft 4.
	/// </summary>
	public static void Enable()
	{
		SchemaKeywordRegistry.Register<Draft4ExclusiveMaximumKeyword>(GraeaeSerializerContext.Default);
		SchemaKeywordRegistry.Register<Draft4ExclusiveMinimumKeyword>(GraeaeSerializerContext.Default);
		SchemaKeywordRegistry.Register<Draft4IdKeyword>(GraeaeSerializerContext.Default);
		SchemaKeywordRegistry.Register<NullableKeyword>(GraeaeSerializerContext.Default);
		SchemaKeywordRegistry.Register<Draft4TypeKeyword>(GraeaeSerializerContext.Default);

		Draft4MetaSchema = new JsonSchemaBuilder()
			.OasId(Draft4MetaSchemaUri)
			.Schema(Draft4MetaSchemaUri)
			.Description("Core schema meta-schema")
			.Definitions(
				("schemaArray", new JsonSchemaBuilder()
					.Type(SchemaValueType.Array)
					.MinItems(1)
					.Items(JsonSchemaBuilder.RefRoot())
				),
				("positiveInteger", new JsonSchemaBuilder()
					.Type(SchemaValueType.Integer)
					.Minimum(0)
				),
				("positiveIntegerDefault0", new JsonSchemaBuilder().Ref("#/definitions/positiveInteger")),
				("simpleTypes", new JsonSchemaBuilder()
					.Enum("array", "boolean", "integer", "null", "number", "object", "string")),
				("stringArray", new JsonSchemaBuilder()
					.Type(SchemaValueType.Array)
					.Items(new JsonSchemaBuilder().Type(SchemaValueType.String))
					.MinItems(1)
					.UniqueItems(true)
				)
			)
			.Type(SchemaValueType.Object)
			.Properties(
				("id", new JsonSchemaBuilder().Type(SchemaValueType.String)),
				("$schema", new JsonSchemaBuilder().Type(SchemaValueType.String)),
				("title", new JsonSchemaBuilder().Type(SchemaValueType.String)),
				("description", new JsonSchemaBuilder().Type(SchemaValueType.String)),
				("default", JsonSchema.Empty),
				("multipleOf", new JsonSchemaBuilder()
					.Type(SchemaValueType.Number)
					.Minimum(0)
					.ExclusiveMinimum(true)
				),
				("maximum", new JsonSchemaBuilder().Type(SchemaValueType.Number)),
				("exclusiveMaximum", new JsonSchemaBuilder()
					.Type(SchemaValueType.Boolean)
					.Default(false)
				),
				("minimum", new JsonSchemaBuilder().Type(SchemaValueType.Number)),
				("exclusiveMinimum", new JsonSchemaBuilder()
					.Type(SchemaValueType.Boolean)
					.Default(false)
				),
				("maxLength", new JsonSchemaBuilder().Ref("#/definitions/positiveInteger")),
				("minLength", new JsonSchemaBuilder().Ref("#/definitions/positiveIntegerDefault0")),
				("pattern", new JsonSchemaBuilder()
					.Type(SchemaValueType.String)
					.Format(Json.Schema.Formats.Regex)
				),
				("additionalItems", new JsonSchemaBuilder()
					.AnyOf(
						new JsonSchemaBuilder().Type(SchemaValueType.Boolean),
						JsonSchemaBuilder.RefRoot()
					)
					.Default(new JsonObject())
				),
				("items", new JsonSchemaBuilder()
					.AnyOf(
						JsonSchemaBuilder.RefRoot(),
						new JsonSchemaBuilder().Ref("#/definitions/schemaArray")
					)
				),
				("maxItems", new JsonSchemaBuilder().Ref("#/definitions/positiveInteger")),
				("minItems", new JsonSchemaBuilder().Ref("#/definitions/positiveIntegerDefault0")),
				("uniqueItems", new JsonSchemaBuilder()
					.Type(SchemaValueType.Boolean)
					.Default(false)
				),
				("maxProperties", new JsonSchemaBuilder().Ref("#/definitions/positiveInteger")),
				("minProperties", new JsonSchemaBuilder().Ref("#/definitions/positiveIntegerDefault0")),
				("required", new JsonSchemaBuilder().Ref("#/definitions/stringArray")),
				("additionalProperties", new JsonSchemaBuilder()
					.AnyOf(
						new JsonSchemaBuilder().Type(SchemaValueType.Boolean),
						JsonSchemaBuilder.RefRoot()
					)
					.Default(new JsonObject())
				),
				("definitions", new JsonSchemaBuilder()
					.Type(SchemaValueType.Object)
					.AdditionalProperties(JsonSchemaBuilder.RefRoot())
					.Default(new JsonObject())
				),
				("properties", new JsonSchemaBuilder()
					.Type(SchemaValueType.Object)
					.AdditionalProperties(JsonSchemaBuilder.RefRoot())
					.Default(new JsonObject())
				),
				("patternProperties", new JsonSchemaBuilder()
					.Type(SchemaValueType.Object)
					.AdditionalProperties(JsonSchemaBuilder.RefRoot())
					.Default(new JsonObject())
				),
				("dependencies", new JsonSchemaBuilder()
					.Type(SchemaValueType.Object)
					.AdditionalProperties(new JsonSchemaBuilder()
						.AnyOf(
							JsonSchemaBuilder.RefRoot(),
							new JsonSchemaBuilder().Ref("#/definitions/stringArray")
						)
					)
				),
				("enum", new JsonSchemaBuilder()
					.Type(SchemaValueType.Array)
					.MinItems(1)
					.UniqueItems(true)
				),
				("type", new JsonSchemaBuilder()
					.AnyOf(
						new JsonSchemaBuilder().Ref("#/definitions/simpleTypes"),
						new JsonSchemaBuilder()
							.Type(SchemaValueType.Array)
							.Items(new JsonSchemaBuilder().Ref("#/definitions/simpleTypes"))
							.MinItems(1)
							.UniqueItems(true)
					)
				),
				("format", new JsonSchemaBuilder().Type(SchemaValueType.String)),
				("allOf", new JsonSchemaBuilder().Ref("#/definitions/schemaArray")),
				("anyOf", new JsonSchemaBuilder().Ref("#/definitions/schemaArray")),
				("oneOf", new JsonSchemaBuilder().Ref("#/definitions/schemaArray")),
				("not", JsonSchemaBuilder.RefRoot())
			)
			.Dependencies(
				("exclusiveMaximum", new[] { "maximum" }),
				("exclusiveMinimum", new[] { "minimum" })
			)
			.Default(new JsonObject());
		Draft4MetaSchema.BaseUri = new Uri(Draft4MetaSchemaUri);
		// This allows draft 4 to be used as a meta-schema.
		SchemaRegistry.RegisterNewSpecVersion(Draft4MetaSchema.BaseUri, Draft4Version);

		SchemaRegistry.Global.Register(Draft4MetaSchema);
	}
}
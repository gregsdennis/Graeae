using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;

namespace Graeae.Models.SchemaDraft4;

/// <summary>
/// Provides the OpenAPI `nullable` keyword.
/// </summary>
[SchemaKeyword(Name)]
[SchemaSpecVersion(Draft4Support.Draft4Version)]
[JsonConverter(typeof(NullableKeywordJsonConverter))]
public class NullableKeyword : IJsonSchemaKeyword
{
	/// <summary>
	/// The name of the keyword.
	/// </summary>
	public const string Name = "nullable";

	/// <summary>
	/// The ID.
	/// </summary>
	public bool Value { get; }

	/// <summary>
	/// Creates a new <see cref="IdKeyword"/>.
	/// </summary>
	/// <param name="value">Whether the `minimum` value should be considered exclusive.</param>
	public NullableKeyword(bool value)
	{
		Value = value;
	}

	/// <summary>Builds a constraint object for a keyword.</summary>
	/// <param name="schemaConstraint">The <see cref="T:Json.Schema.SchemaConstraint" /> for the schema object that houses this keyword.</param>
	/// <param name="localConstraints">
	/// The set of other <see cref="T:Json.Schema.KeywordConstraint" />s that have been processed prior to this one.
	/// Will contain the constraints for keyword dependencies.
	/// </param>
	/// <param name="context">The <see cref="T:Json.Schema.EvaluationContext" />.</param>
	/// <returns>A constraint object.</returns>
	public KeywordConstraint GetConstraint(SchemaConstraint schemaConstraint, IReadOnlyList<KeywordConstraint> localConstraints, EvaluationContext context)
	{
		return new KeywordConstraint(Name, Evaluator);
	}

	private void Evaluator(KeywordEvaluation evaluation, EvaluationContext context)
	{
		var schemaValueType = evaluation.LocalInstance.GetSchemaValueType();
		if (schemaValueType == SchemaValueType.Null && !Value) 
			evaluation.Results.Fail(Name, "nulls are not allowed"); // TODO: localize error message

	}
}

internal class NullableKeywordJsonConverter : JsonConverter<NullableKeyword>
{
	public override NullableKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not (JsonTokenType.True or JsonTokenType.False))
		{
			throw new JsonException("Expected boolean");
		}

		return new NullableKeyword(reader.GetBoolean());
	}

	public override void Write(Utf8JsonWriter writer, NullableKeyword value, JsonSerializerOptions options)
	{
		writer.WriteBoolean(NullableKeyword.Name, value.Value);
	}
}
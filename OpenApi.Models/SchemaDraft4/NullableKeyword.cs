using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;

namespace OpenApi.Models.SchemaDraft4;

[SchemaKeyword(Name)]
[SchemaSpecVersion(Draft4Support.Draft4Version)]
[JsonConverter(typeof(NullableKeywordJsonConverter))]
public class NullableKeyword : IJsonSchemaKeyword
{
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
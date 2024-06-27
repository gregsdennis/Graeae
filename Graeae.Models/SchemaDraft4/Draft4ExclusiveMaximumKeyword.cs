using System.Text.Json;
using System.Text.Json.Serialization;
using Json.More;
using Json.Schema;

namespace Graeae.Models.SchemaDraft4;

/// <summary>
/// Overrides the JSON Schema <see cref="ExclusiveMaximumKeyword"/> to support draft 4 boolean values.
/// </summary>
[SchemaKeyword(Name)]
[SchemaSpecVersion(Draft4Support.Draft4Version)]
[SchemaSpecVersion(SpecVersion.Draft202012)]
[DependsOnAnnotationsFrom(typeof(MinimumKeyword))]
[JsonConverter(typeof(Draft4ExclusiveMaximumKeywordJsonConverter))]
public class Draft4ExclusiveMaximumKeyword : IJsonSchemaKeyword
{
	/// <summary>
	/// The name of the keyword.
	/// </summary>
	public const string Name = "exclusiveMaximum";

	private readonly ExclusiveMaximumKeyword? _numberSupport;

	/// <summary>
	/// The boolean value, if it's a boolean.
	/// </summary>
	public bool? BoolValue { get; }

	/// <summary>
	/// The number value, if it's a number.
	/// </summary>
	public decimal? NumberValue => _numberSupport?.Value;

	/// <summary>
	/// Creates a new <see cref="IdKeyword"/>.
	/// </summary>
	/// <param name="value">Whether the `minimum` value should be considered exclusive.</param>
	public Draft4ExclusiveMaximumKeyword(bool value)
	{
		BoolValue = value;
	}

	/// <summary>
	/// Creates a new <see cref="IdKeyword"/>.
	/// </summary>
	/// <param name="value">The minimum value.</param>
	public Draft4ExclusiveMaximumKeyword(decimal value)
	{
		_numberSupport = new ExclusiveMaximumKeyword(value);
	}

	/// <summary>Builds a constraint object for a keyword.</summary>
	/// <param name="schemaConstraint">The <see cref="T:Json.Schema.SchemaConstraint" /> for the schema object that houses this keyword.</param>
	/// <param name="localConstraints">
	/// The set of other <see cref="T:Json.Schema.KeywordConstraint" />s that have been processed prior to this one.
	/// Will contain the constraints for keyword dependencies.
	/// </param>
	/// <param name="context">The <see cref="T:Json.Schema.EvaluationContext" />.</param>
	/// <returns>A constraint object.</returns>
	public KeywordConstraint GetConstraint(SchemaConstraint schemaConstraint, ReadOnlySpan<KeywordConstraint> localConstraints, EvaluationContext context)
	{
		if (BoolValue.HasValue)
		{
			if (!BoolValue.Value) return KeywordConstraint.Skip;

			var maximumConstraint = localConstraints.GetKeywordConstraint<MaximumKeyword>();
			if (maximumConstraint == null) return KeywordConstraint.Skip;

			var value = schemaConstraint.LocalSchema.GetMaximum()!.Value;
			return new KeywordConstraint(Name, (e, c) => Evaluator(e, c, value))
			{
				SiblingDependencies = new[] { maximumConstraint }
			};
		}

		return _numberSupport!.GetConstraint(schemaConstraint, localConstraints, context);
	}

	private void Evaluator(KeywordEvaluation evaluation, EvaluationContext context, decimal limit)
	{
		var schemaValueType = evaluation.LocalInstance.GetSchemaValueType();
		if (schemaValueType is not (SchemaValueType.Number or SchemaValueType.Integer))
		{
			evaluation.MarkAsSkipped();
			return;
		}

		var number = evaluation.LocalInstance!.AsValue().GetNumber();

		if (number >= limit)
			evaluation.Results.Fail(Name, ErrorMessages.GetExclusiveMaximum(context.Options.Culture), ("received", number), ("limit", BoolValue));
	}
}

internal class Draft4ExclusiveMaximumKeywordJsonConverter : JsonConverter<Draft4ExclusiveMaximumKeyword>
{
	public override Draft4ExclusiveMaximumKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return reader.TokenType switch
		{
			JsonTokenType.True or JsonTokenType.False => new Draft4ExclusiveMaximumKeyword(reader.GetBoolean()),
			JsonTokenType.Number => new Draft4ExclusiveMaximumKeyword(reader.GetDecimal()),
			_ => throw new JsonException("Expected boolean or number")
		};
	}

	public override void Write(Utf8JsonWriter writer, Draft4ExclusiveMaximumKeyword value, JsonSerializerOptions options)
	{
		if (value.BoolValue.HasValue)
		{
			writer.WriteBoolean(Draft4ExclusiveMaximumKeyword.Name, value.BoolValue.Value);
		}
		else
		{
			writer.WriteNumber(Draft4ExclusiveMaximumKeyword.Name, value.NumberValue!.Value);
		}
	}
}
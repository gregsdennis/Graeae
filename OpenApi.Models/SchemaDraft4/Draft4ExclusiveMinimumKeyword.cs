using System.Text.Json;
using System.Text.Json.Serialization;
using Json.More;
using Json.Schema;

namespace OpenApi.Models.SchemaDraft4;

[SchemaKeyword(Name)]
[SchemaSpecVersion(Draft4Support.Draft4Version)]
[SchemaSpecVersion(SpecVersion.Draft202012)]
[JsonConverter(typeof(Draft4ExclusiveMinimumKeywordJsonConverter))]
public class Draft4ExclusiveMinimumKeyword : IJsonSchemaKeyword
{
	public const string Name = "exclusiveMinimum";

	private readonly ExclusiveMinimumKeyword? _numberSupport;

	/// <summary>
	/// The ID.
	/// </summary>
	public bool? BoolValue { get; }

	public decimal? NumberValue => _numberSupport?.Value;

	/// <summary>
	/// Creates a new <see cref="IdKeyword"/>.
	/// </summary>
	/// <param name="value">Whether the `minimum` value should be considered exclusive.</param>
	public Draft4ExclusiveMinimumKeyword(bool value)
	{
		BoolValue = value;
	}

	public Draft4ExclusiveMinimumKeyword(decimal value)
	{
		_numberSupport = new ExclusiveMinimumKeyword(value);
	}

	public KeywordConstraint GetConstraint(SchemaConstraint schemaConstraint, IReadOnlyList<KeywordConstraint> localConstraints, EvaluationContext context)
	{
		if (BoolValue.HasValue)
		{
			if (!BoolValue.Value) return KeywordConstraint.Skip;

			var minimumConstraint = localConstraints.SingleOrDefault(x => x.Keyword == MinimumKeyword.Name);
			if (minimumConstraint == null) return KeywordConstraint.Skip;

			var localSchema = schemaConstraint.GetLocalSchema(context.Options);

			var value = localSchema.GetMinimum()!.Value;
			return new KeywordConstraint(Name, (e, c) => Evaluator(e, c, value))
			{
				SiblingDependencies = new[] { minimumConstraint }
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
			evaluation.Results.Fail(Name, ErrorMessages.GetExclusiveMinimum(context.Options.Culture), ("received", number), ("limit", BoolValue));
	}
}

internal class Draft4ExclusiveMinimumKeywordJsonConverter : JsonConverter<Draft4ExclusiveMinimumKeyword>
{
	public override Draft4ExclusiveMinimumKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return reader.TokenType switch
		{
			JsonTokenType.True or JsonTokenType.False => new Draft4ExclusiveMinimumKeyword(reader.GetBoolean()),
			JsonTokenType.Number => new Draft4ExclusiveMinimumKeyword(reader.GetDecimal()),
			_ => throw new JsonException("Expected boolean or number")
		};
	}

	public override void Write(Utf8JsonWriter writer, Draft4ExclusiveMinimumKeyword value, JsonSerializerOptions options)
	{
		if (value.BoolValue.HasValue)
			writer.WriteBoolean(Draft4ExclusiveMinimumKeyword.Name, value.BoolValue.Value);
		else
			writer.WriteNumber(Draft4ExclusiveMinimumKeyword.Name, value.NumberValue!.Value);
	}
}
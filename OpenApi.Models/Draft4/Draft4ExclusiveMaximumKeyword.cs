﻿using System.Text.Json;
using System.Text.Json.Serialization;
using Json.More;
using Json.Schema;

namespace OpenApi.Models.Draft4;

[SchemaKeyword(Name)]
[SchemaSpecVersion(Draft4Support.Draft4Version)]
[SchemaSpecVersion(SpecVersion.Draft202012)]
[JsonConverter(typeof(Draft4ExclusiveMaximumKeywordJsonConverter))]
public class Draft4ExclusiveMaximumKeyword : IJsonSchemaKeyword, IEquatable<Draft4ExclusiveMaximumKeyword>
{
	public const string Name = "exclusiveMaximum";

	private readonly ExclusiveMaximumKeyword? _numberSupport;

	/// <summary>
	/// The ID.
	/// </summary>
	public bool? BoolValue { get; }

	public decimal? NumberValue => _numberSupport?.Value;

	/// <summary>
	/// Creates a new <see cref="IdKeyword"/>.
	/// </summary>
	/// <param name="value">Whether the `minimum` value should be considered exclusive.</param>
	public Draft4ExclusiveMaximumKeyword(bool value)
	{
		BoolValue = value;
	}

	public Draft4ExclusiveMaximumKeyword(decimal value)
	{
		_numberSupport = new ExclusiveMaximumKeyword(value);
	}

	public void Evaluate(EvaluationContext context)
	{
		// TODO: do we need to validate that the right version of the keyword is being used?
		if (BoolValue.HasValue)
		{
			context.EnterKeyword(Name);
			if (!BoolValue.Value)
			{
				context.NotApplicable(() => "exclusiveMinimum is false; minimum validation is sufficient");
				return;
			}

			var limit = context.LocalSchema.GetMinimum();
			if (!limit.HasValue)
			{
				context.NotApplicable(() => "minimum not present");
				return;
			}

			var schemaValueType = context.LocalInstance.GetSchemaValueType();
			if (schemaValueType is not (SchemaValueType.Number or SchemaValueType.Integer))
			{
				context.WrongValueKind(schemaValueType);
				return;
			}

			var number = context.LocalInstance!.AsValue().GetNumber();

			if (limit == number)
				context.LocalResult.Fail(Name, ErrorMessages.ExclusiveMaximum, ("received", number), ("limit", BoolValue));
			context.ExitKeyword(Name, context.LocalResult.IsValid);
		}
		else
		{
			_numberSupport!.Evaluate(context);
		}
	}

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
	public bool Equals(Draft4ExclusiveMaximumKeyword? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return Equals(BoolValue, other.BoolValue);
	}

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
	public override bool Equals(object? obj)
	{
		return Equals(obj as Draft4ExclusiveMaximumKeyword);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		return BoolValue.GetHashCode();
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
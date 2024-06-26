﻿using System.Text.Json;
using System.Text.Json.Serialization;
using Json.More;
using Json.Schema;

namespace Graeae.Models.SchemaDraft4;

/// <summary>
/// Overrides the JSON Schema <see cref="TypeKeyword"/> to support draft 4.
/// </summary>
[SchemaKeyword(Name)]
[SchemaSpecVersion(Draft4Support.Draft4Version)]
[SchemaSpecVersion(SpecVersion.Draft202012)]
[JsonConverter(typeof(Draft4TypeKeywordConverter))]
public class Draft4TypeKeyword : IJsonSchemaKeyword
{
	/// <summary>
	/// The name of the keyword.
	/// </summary>
	public const string Name = "type";

	private readonly TypeKeyword _basicSupport;
	private readonly TypeKeyword _draft4Support;

	/// <summary>
	/// The ID.
	/// </summary>
	public SchemaValueType Type => _basicSupport.Type;

	/// <summary>
	/// Creates a new <see cref="IdKeyword"/>.
	/// </summary>
	/// <param name="type">The instance type that is allowed.</param>
	public Draft4TypeKeyword(SchemaValueType type)
	{
		_basicSupport = new TypeKeyword(type);
		_draft4Support = new TypeKeyword(type | SchemaValueType.Null);
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
		return context.Options.EvaluateAs == Draft4Support.Draft4Version
			? _draft4Support.GetConstraint(schemaConstraint, localConstraints, context)
			: _basicSupport.GetConstraint(schemaConstraint, localConstraints, context);
	}
}

/// <summary>
/// JSON converter for <see cref="Draft4TypeKeyword"/>
/// </summary>
public class Draft4TypeKeywordConverter : WeaklyTypedJsonConverter<Draft4TypeKeyword>
{
	/// <summary>Reads and converts the JSON to type <typeparamref name="T" />.</summary>
	/// <param name="reader">The reader.</param>
	/// <param name="typeToConvert">The type to convert.</param>
	/// <param name="options">An object that specifies serialization options to use.</param>
	/// <returns>The converted value.</returns>
	public override Draft4TypeKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var type = options.Read(ref reader, GraeaeSerializerContext.Default.SchemaValueType);

		return new Draft4TypeKeyword(type);
	}

	/// <summary>Writes a specified value as JSON.</summary>
	/// <param name="writer">The writer to write to.</param>
	/// <param name="value">The value to convert to JSON.</param>
	/// <param name="options">An object that specifies serialization options to use.</param>
	public override void Write(Utf8JsonWriter writer, Draft4TypeKeyword value, JsonSerializerOptions options)
	{
		options.Write(writer, value.Type, GraeaeSerializerContext.Default.SchemaValueType);
	}
}
﻿using System.Text.Json;
using System.Text.Json.Serialization;
using Json.More;
using Json.Schema;

namespace Graeae.Models.SchemaDraft4;

/// <summary>
/// Represents the JSON Schema draft 4 `id` keyword.
/// </summary>
[SchemaKeyword(Name)]
[SchemaSpecVersion(Draft4Support.Draft4Version)]
[JsonConverter(typeof(Draft4IdKeywordJsonConverter))]
public class Draft4IdKeyword : IIdKeyword
{
	/// <summary>
	/// The name of the keyword.
	/// </summary>
	public const string Name = "id";

	/// <summary>
	/// The ID.
	/// </summary>
	public Uri Id { get; }

	/// <summary>
	/// Creates a new <see cref="IdKeyword"/>.
	/// </summary>
	/// <param name="id">The ID.</param>
	public Draft4IdKeyword(Uri id)
	{
		Id = id ?? throw new ArgumentNullException(nameof(id));
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
		return KeywordConstraint.Skip;
	}
}

/// <summary>
/// JSON converter for <see cref="Draft4IdKeyword"/>
/// </summary>
public class Draft4IdKeywordJsonConverter : WeaklyTypedJsonConverter<Draft4IdKeyword>
{
	/// <summary>Reads and converts the JSON to type <typeparamref name="T" />.</summary>
	/// <param name="reader">The reader.</param>
	/// <param name="typeToConvert">The type to convert.</param>
	/// <param name="options">An object that specifies serialization options to use.</param>
	/// <returns>The converted value.</returns>
	public override Draft4IdKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.String)
			throw new JsonException("Expected string");

		var uriString = reader.GetString();
		if (!Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out var uri))
			throw new JsonException("Expected URI");

		return new Draft4IdKeyword(uri);
	}

	/// <summary>Writes a specified value as JSON.</summary>
	/// <param name="writer">The writer to write to.</param>
	/// <param name="value">The value to convert to JSON.</param>
	/// <param name="options">An object that specifies serialization options to use.</param>
	public override void Write(Utf8JsonWriter writer, Draft4IdKeyword value, JsonSerializerOptions options)
	{
		writer.WriteString(Draft4IdKeyword.Name, value.Id.OriginalString);
	}
}
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;

namespace OpenApi.Models.SchemaDraft4;

[SchemaKeyword(Name)]
[SchemaSpecVersion(Draft4Support.Draft4Version)]
[JsonConverter(typeof(Draft4IdKeywordJsonConverter))]
public class Draft4IdKeyword : IIdKeyword
{
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

	public KeywordConstraint GetConstraint(SchemaConstraint schemaConstraint, IReadOnlyList<KeywordConstraint> localConstraints, EvaluationContext context)
	{
		return KeywordConstraint.Skip;
	}
}

internal class Draft4IdKeywordJsonConverter : JsonConverter<Draft4IdKeyword>
{
	public override Draft4IdKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.String)
			throw new JsonException("Expected string");

		var uriString = reader.GetString();
		if (!Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out var uri))
			throw new JsonException("Expected URI");

		return new Draft4IdKeyword(uri);
	}

	public override void Write(Utf8JsonWriter writer, Draft4IdKeyword value, JsonSerializerOptions options)
	{
		writer.WriteString(Draft4IdKeyword.Name, value.Id.OriginalString);
	}
}
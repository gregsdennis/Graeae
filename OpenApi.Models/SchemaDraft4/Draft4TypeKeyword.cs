using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;

namespace OpenApi.Models.SchemaDraft4;

[SchemaKeyword(Name)]
[SchemaSpecVersion(Draft4Support.Draft4Version)]
[SchemaSpecVersion(SpecVersion.Draft202012)]
[JsonConverter(typeof(Draft4TypeKeywordConverter))]
public class Draft4TypeKeyword : IJsonSchemaKeyword
{
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

	public KeywordConstraint GetConstraint(SchemaConstraint schemaConstraint, IReadOnlyList<KeywordConstraint> localConstraints, EvaluationContext context)
	{
		return context.Options.EvaluateAs == Draft4Support.Draft4Version
			? _draft4Support.GetConstraint(schemaConstraint, localConstraints, context)
			: _basicSupport.GetConstraint(schemaConstraint, localConstraints, context);
	}
}

internal class Draft4TypeKeywordConverter : JsonConverter<Draft4TypeKeyword>
{
	public override Draft4TypeKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var type = JsonSerializer.Deserialize<SchemaValueType>(ref reader, options);

		return new Draft4TypeKeyword(type);
	}
	public override void Write(Utf8JsonWriter writer, Draft4TypeKeyword value, JsonSerializerOptions options)
	{
		writer.WritePropertyName(Draft4TypeKeyword.Name);
		JsonSerializer.Serialize(writer, value.Type, options);
	}
}
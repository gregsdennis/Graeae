using Json.Schema;

namespace Graeae.Models.SchemaDraft4;

/// <summary>
/// Provides extension methods for explicitly building draft 4 schemas.
/// </summary>
public static class JsonSchemaKeywordExtensions
{
	/// <summary>
	/// Adds the draft 4 `exclusiveMaximum` override.
	/// </summary>
	/// <param name="builder">The builder</param>
	/// <param name="value">The value</param>
	/// <returns>The builder</returns>
	public static JsonSchemaBuilder ExclusiveMaximum(this JsonSchemaBuilder builder, bool value)
	{
		builder.Add(new Draft4ExclusiveMaximumKeyword(value));
		return builder;
	}

	/// <summary>
	/// Adds the draft 4 `exclusiveMinimum` override.
	/// </summary>
	/// <param name="builder">The builder</param>
	/// <param name="value">The value</param>
	/// <returns>The builder</returns>
	public static JsonSchemaBuilder ExclusiveMinimum(this JsonSchemaBuilder builder, bool value)
	{
		builder.Add(new Draft4ExclusiveMinimumKeyword(value));
		return builder;
	}

	/// <summary>
	/// Adds the draft 4 `id` override.
	/// </summary>
	/// <param name="builder">The builder</param>
	/// <param name="id">The id</param>
	/// <returns>The builder</returns>
	public static JsonSchemaBuilder OasId(this JsonSchemaBuilder builder, Uri id)
	{
		builder.Add(new Draft4IdKeyword(id));
		return builder;
	}

	/// <summary>
	/// Adds the draft 4 `id` override.
	/// </summary>
	/// <param name="builder">The builder</param>
	/// <param name="id">The id</param>
	/// <returns>The builder</returns>
	public static JsonSchemaBuilder OasId(this JsonSchemaBuilder builder, string id)
	{
		builder.Add(new Draft4IdKeyword(new Uri(id, UriKind.RelativeOrAbsolute)));
		return builder;
	}

	/// <summary>
	/// Adds the draft 4 `type` override.
	/// </summary>
	/// <param name="builder">The builder</param>
	/// <param name="type">The type</param>
	/// <returns>The builder</returns>
	public static JsonSchemaBuilder OasType(this JsonSchemaBuilder builder, SchemaValueType type)
	{
		builder.Add(new Draft4TypeKeyword(type));
		return builder;
	}

	/// <summary>
	/// Adds the `nullable` keyword.
	/// </summary>
	/// <param name="builder">The builder</param>
	/// <param name="value">The value</param>
	/// <returns>The builder</returns>
	public static JsonSchemaBuilder Nullable(this JsonSchemaBuilder builder, bool value)
	{
		builder.Add(new NullableKeyword(value));
		return builder;
	}
}
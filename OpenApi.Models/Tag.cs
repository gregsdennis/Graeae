using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OpenApi.Models;

/// <summary>
/// Models a tag.
/// </summary>
[JsonConverter(typeof(TagJsonConverter))]
public class Tag : IRefTargetContainer
{
	private static readonly string[] KnownKeys =
	{
		"name",
		"description",
		"externalDocs"
	};

	/// <summary>
	/// Gets the tag name.
	/// </summary>
	public string Name { get; }
	/// <summary>
	/// Gets or sets the tag description.
	/// </summary>
	public string? Description { get; set; }
	/// <summary>
	/// Gets or sets external documentation.
	/// </summary>
	public ExternalDocumentation? ExternalDocs { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	/// <summary>
	/// Creates a new <see cref="Tag"/>
	/// </summary>
	/// <param name="name">The tag name</param>
	public Tag(string name)
	{
		Name = name;
	}

	/// <summary>
	/// Creates a new <see cref="Tag"/> from a <see cref="JsonNode"/>.
	/// </summary>
	/// <param name="node">The `JsonNode`.</param>
	/// <returns>The model.</returns>
	/// <exception cref="JsonException">Thrown when the JSON does not accurately represent the model.</exception>
	internal static Tag FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var tag = new Tag(obj.ExpectString("name", "tag"))
		{
			Description = obj.MaybeString("description", "tag"),
			ExternalDocs = obj.Maybe("externalDocs", ExternalDocumentation.FromNode),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, tag.ExtensionData?.Keys);

		return tag;
	}

	internal static JsonNode? ToNode(Tag? tag)
	{
		if (tag == null) return null;

		var obj = new JsonObject
		{
			["name"] = tag.Name
		};

		obj.MaybeAdd("description", tag.Description);
		obj.MaybeAdd("externalDocs", ExternalDocumentation.ToNode(tag.ExternalDocs));
		obj.AddExtensions(tag.ExtensionData);

		return obj;
	}

	object? IRefTargetContainer.Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		if (keys[0] == "externalDocs")
		{
			if (keys.Length == 1) return ExternalDocs;
			return ExternalDocs?.Resolve(keys[1..]);
		}

		return ExtensionData?.Resolve(keys);
	}
}

internal class TagJsonConverter : JsonConverter<Tag>
{
	public override Tag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return Tag.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, Tag value, JsonSerializerOptions options)
	{
		var json = Tag.ToNode(value);

		JsonSerializer.Serialize(writer, json, options);
	}
}
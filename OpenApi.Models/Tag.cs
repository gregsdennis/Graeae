using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenApi.Models;

public class Tag : IRefResolvable
{
	private static readonly string[] KnownKeys =
	{
		"name",
		"description",
		"externalDocs"
	};

	public string Name { get; set; }
	public string? Description { get; set; }
	public ExternalDocumentation? ExternalDocs { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public static Tag FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var tag = new Tag
		{
			Name = obj.ExpectString("name", "tag"),
			Description = obj.MaybeString("description", "tag"),
			ExternalDocs = obj.Maybe("externalDocs", ExternalDocumentation.FromNode),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, tag.ExtensionData?.Keys);

		return tag;
	}

	public static JsonNode? ToNode(Tag? tag)
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

	public object? Resolve(Span<string> keys)
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
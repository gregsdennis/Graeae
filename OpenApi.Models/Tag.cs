using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenApi.Models;

public class Tag
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

	public static Tag FromNode(JsonNode? node, JsonSerializerOptions? options)
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
}
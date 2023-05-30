using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenApi.Models;

public class ExternalDocumentation
{
	private static readonly string[] KnownKeys =
	{
		"description",
		"url"
	};

	public string? Description { get; set; }
	public Uri Url { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public static ExternalDocumentation FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var docs = new ExternalDocumentation
		{
			Description = obj.MaybeString("description", "external documentation"),
			Url = obj.ExpectUri("url", "external documentation"),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, docs.ExtensionData?.Keys);

		return docs;
	}

	public static JsonNode? ToNode(ExternalDocumentation? docs)
	{
		if (docs == null) return null;

		var obj = new JsonObject
		{
			["url"] = docs.Url.ToString()
		};

		obj.MaybeAdd("description", docs.Description);
		obj.AddExtensions(docs.ExtensionData);

		return obj;
	}
}
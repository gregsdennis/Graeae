using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OpenApi.Models;

[JsonConverter(typeof(ExternalDocumentationJsonConverter))]
public class ExternalDocumentation : IRefTargetContainer
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

	public object? Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		return ExtensionData?.Resolve(keys);
	}
}

public class ExternalDocumentationJsonConverter : JsonConverter<ExternalDocumentation>
{
	public override ExternalDocumentation? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return ExternalDocumentation.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, ExternalDocumentation value, JsonSerializerOptions options)
	{
		var json = ExternalDocumentation.ToNode(value);

		JsonSerializer.Serialize(writer, json, options);
	}
}

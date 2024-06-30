using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Graeae.Models;

/// <summary>
/// Models external documentation.
/// </summary>
[JsonConverter(typeof(ExternalDocumentationJsonConverter))]
public class ExternalDocumentation : IRefTargetContainer
{
	private static readonly string[] KnownKeys =
	{
		"description",
		"url"
	};

	/// <summary>
	/// Gets or sets the description.
	/// </summary>
	public string? Description { get; set; }
	/// <summary>
	/// Gets the URL for the target documentation.
	/// </summary>
	public Uri Url { get; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	/// <summary>
	/// Creates a new <see cref="ExternalDocumentation"/>
	/// </summary>
	/// <param name="url">The URL for the target documentation.</param>
	public ExternalDocumentation(Uri url)
	{
		Url = url;
	}

	/// <summary>
	/// Creates a new <see cref="ExternalDocumentation"/>
	/// </summary>
	/// <param name="url">The URL for the target documentation.</param>
	public ExternalDocumentation(string url)
	{
		Url = new Uri(url, UriKind.RelativeOrAbsolute);
	}

	internal static ExternalDocumentation FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var docs = new ExternalDocumentation(obj.ExpectUri("url", "external documentation"))
		{
			Description = obj.MaybeString("description", "external documentation"),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, docs.ExtensionData?.Keys);

		return docs;
	}

	internal static JsonNode? ToNode(ExternalDocumentation? docs)
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

	object? IRefTargetContainer.Resolve(ReadOnlySpan<string> keys)
	{
		if (keys.Length == 0) return this;

		return ExtensionData?.Resolve(keys);
	}
}

internal class ExternalDocumentationJsonConverter : JsonConverter<ExternalDocumentation>
{
	public override ExternalDocumentation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

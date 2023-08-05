using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OpenApi.Models;

/// <summary>
/// Models the info object.
/// </summary>
[JsonConverter(typeof(OpenApiInfoJsonConverter))]
public class OpenApiInfo : IRefTargetContainer
{
	private static readonly string[] KnownKeys =
	{
		"title",
		"summary",
		"description",
		"termsOfService",
		"contact",
		"license",
		"version"
	};

	public string Title { get; }
	public string? Summary { get; set; }
	public string? Description { get; set; }
	public string? TermsOfService { get; set; }
	public ContactInfo? Contact { get; set; }
	public LicenseInfo? License { get; set; }
	public string Version { get; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	public OpenApiInfo(string title, string version)
	{
		Title = title;
		Version = version;
	}

	public static OpenApiInfo FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var info = new OpenApiInfo(
			obj.ExpectString("title", "open api info"),
			obj.ExpectString("version", "open api info"))
		{
			Summary = obj.MaybeString("summary", "open api info"),
			Description = obj.MaybeString("description", "open api info"),
			TermsOfService = obj.MaybeString("termsOfService", "open api info"),
			Contact = obj.Maybe("contact", ContactInfo.FromNode),
			License = obj.Maybe("license", LicenseInfo.FromNode),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, info.ExtensionData?.Keys);

		return info;
	}

	public static JsonNode? ToNode(OpenApiInfo? info)
	{
		if (info == null) return null;

		var obj = new JsonObject
		{
			["title"] = info.Title,
			["version"] = info.Version
		};

		obj.MaybeAdd("summary", info.Summary);
		obj.MaybeAdd("description", info.Description);
		obj.MaybeAdd("termsOfService", info.TermsOfService);
		obj.MaybeAdd("contact", ContactInfo.ToNode(info.Contact));
		obj.MaybeAdd("license", LicenseInfo.ToNode(info.License));
		obj.AddExtensions(info.ExtensionData);

		return obj;
	}

	public object? Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		int keysConsumed = 1;
		IRefTargetContainer? target = null;
		switch (keys[0])
		{
			case "contact":
				target = Contact;
				break;
			case "license":
				target = License;
				break;
		}

		return target != null
			? target.Resolve(keys[keysConsumed..])
			: ExtensionData?.Resolve(keys);
	}
}

internal class OpenApiInfoJsonConverter : JsonConverter<OpenApiInfo>
{
	public override OpenApiInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return OpenApiInfo.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, OpenApiInfo value, JsonSerializerOptions options)
	{
		var json = OpenApiInfo.ToNode(value);

		JsonSerializer.Serialize(writer, json, options);
	}
}

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

	/// <summary>
	/// Gets the title.
	/// </summary>
	public string Title { get; }
	/// <summary>
	/// Gets or sets the summary.
	/// </summary>
	public string? Summary { get; set; }
	/// <summary>
	/// Gets or sets the description.
	/// </summary>
	public string? Description { get; set; }
	/// <summary>
	/// Gets or sets the link to the terms of service.
	/// </summary>
	public Uri? TermsOfService { get; set; }
	/// <summary>
	/// Gets or sets the contact information.
	/// </summary>
	public ContactInfo? Contact { get; set; }
	/// <summary>
	/// Gets or sets the license information.
	/// </summary>
	public LicenseInfo? License { get; set; }
	/// <summary>
	/// Gets or sets the API version.
	/// </summary>
	public string Version { get; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	/// <summary>
	/// Creates a new <see cref="OpenApiInfo"/>
	/// </summary>
	/// <param name="title">The title</param>
	/// <param name="version">The API version</param>
	public OpenApiInfo(string title, string version)
	{
		Title = title;
		Version = version;
	}

	internal static OpenApiInfo FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var info = new OpenApiInfo(
			obj.ExpectString("title", "open api info"),
			obj.ExpectString("version", "open api info"))
		{
			Summary = obj.MaybeString("summary", "open api info"),
			Description = obj.MaybeString("description", "open api info"),
			TermsOfService = obj.MaybeUri("termsOfService", "open api info"),
			Contact = obj.Maybe("contact", ContactInfo.FromNode),
			License = obj.Maybe("license", LicenseInfo.FromNode),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, info.ExtensionData?.Keys);

		return info;
	}

	internal static JsonNode? ToNode(OpenApiInfo? info)
	{
		if (info == null) return null;

		var obj = new JsonObject
		{
			["title"] = info.Title,
			["version"] = info.Version
		};

		obj.MaybeAdd("summary", info.Summary);
		obj.MaybeAdd("description", info.Description);
		obj.MaybeAdd("termsOfService", info.TermsOfService?.ToString());
		obj.MaybeAdd("contact", ContactInfo.ToNode(info.Contact));
		obj.MaybeAdd("license", LicenseInfo.ToNode(info.License));
		obj.AddExtensions(info.ExtensionData);

		return obj;
	}

	object? IRefTargetContainer.Resolve(Span<string> keys)
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

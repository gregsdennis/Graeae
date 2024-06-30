using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Graeae.Models;

/// <summary>
/// Models the license information.
/// </summary>
[JsonConverter(typeof(LicenseInfoJsonConverter))]
public class LicenseInfo : IRefTargetContainer
{
	private static readonly string[] KnownKeys =
	{
		"name",
		"identifier",
		"url"
	};

	/// <summary>
	/// Gets license name used for the API.
	/// </summary>
	public string Name { get; }
	/// <summary>
	/// Gets or sets an SPDX license expression for the API.
	/// </summary>
	public string? Identifier { get; set; }
	/// <summary>
	/// Gets or sets URL to the license used for the API.
	/// </summary>
	public Uri? Url { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	/// <summary>
	/// Creates a new <see cref="LicenseInfo"/>
	/// </summary>
	/// <param name="name">The license name used for the API.</param>
	public LicenseInfo(string name)
	{
		Name = name;
	}

	internal static LicenseInfo FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var info = new LicenseInfo(obj.ExpectString("name", "license info"))
		{
			Identifier = obj.MaybeString("identifier", "license info"),
			Url = obj.MaybeUri("url", "license info"),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, info.ExtensionData?.Keys);

		return info;
	}

	internal static JsonNode? ToNode(LicenseInfo? license)
	{
		if (license == null) return null;

		var obj = new JsonObject
		{
			["name"] = license.Name
		};

		obj.MaybeAdd("identifier", license.Identifier);
		obj.MaybeAdd("url", license.Url?.ToString());
		obj.AddExtensions(license.ExtensionData);

		return obj;
	}

	object? IRefTargetContainer.Resolve(ReadOnlySpan<string> keys)
	{
		if (keys.Length == 0) return this;

		return ExtensionData?.Resolve(keys);
	}
}

internal class LicenseInfoJsonConverter : JsonConverter<LicenseInfo>
{
	public override LicenseInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return LicenseInfo.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, LicenseInfo value, JsonSerializerOptions options)
	{
		var json = LicenseInfo.ToNode(value);

		JsonSerializer.Serialize(writer, json, options);
	}
}

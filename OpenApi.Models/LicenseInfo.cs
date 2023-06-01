using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OpenApi.Models;

[JsonConverter(typeof(LicenseInfoJsonConverter))]
public class LicenseInfo : IRefResolvable
{
	private static readonly string[] KnownKeys =
	{
		"name",
		"identifier",
		"url"
	};

	public string Name { get; set; }
	public string? Identifier { get; set; }
	public Uri? Url { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public static LicenseInfo FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var info = new LicenseInfo
		{
			Name = obj.ExpectString("name", "license info"),
			Identifier = obj.MaybeString("identifier", "license info"),
			Url = obj.MaybeUri("url", "license info"),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, info.ExtensionData?.Keys);

		return info;
	}

	public static JsonNode? ToNode(LicenseInfo? license)
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

	public object? Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		return ExtensionData?.Resolve(keys);
	}
}

public class LicenseInfoJsonConverter : JsonConverter<LicenseInfo>
{
	public override LicenseInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

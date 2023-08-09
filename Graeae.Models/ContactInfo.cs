using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Graeae.Models;

/// <summary>
/// Models the contact information.
/// </summary>
[JsonConverter(typeof(ContactInfoJsonConverter))]
public class ContactInfo : IRefTargetContainer
{
	private static readonly string[] KnownKeys =
	{
		"name",
		"url",
		"email"
	};

	/// <summary>
	/// Gets or sets the contact name.
	/// </summary>
	public string? Name { get; set; }
	/// <summary>
	/// Gets or sets the contact URL.
	/// </summary>
	public Uri? Url { get; set; }
	/// <summary>
	/// Gets or sets the contact email.
	/// </summary>
	public string? Email { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	internal static ContactInfo FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var info = new ContactInfo
		{
			Name = obj.MaybeString("name", "contact info"),
			Url = obj.MaybeUri("url", "contact info"),
			Email = obj.MaybeString("email", "contact info"),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, info.ExtensionData?.Keys);

		return info;
	}

	internal static JsonNode? ToNode(ContactInfo? contact)
	{
		if (contact == null) return null;

		var obj = new JsonObject();

		obj.MaybeAdd("name", contact.Name);
		obj.MaybeAdd("url", contact.Url?.OriginalString);
		obj.MaybeAdd("email", contact.Email);
		obj.AddExtensions(contact.ExtensionData);

		return obj;
	}

	object? IRefTargetContainer.Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		return ExtensionData?.Resolve(keys);
	}
}

internal class ContactInfoJsonConverter : JsonConverter<ContactInfo>
{
	public override ContactInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return ContactInfo.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, ContactInfo value, JsonSerializerOptions options)
	{
		var json = ContactInfo.ToNode(value);

		JsonSerializer.Serialize(writer, json, options);
	}
}

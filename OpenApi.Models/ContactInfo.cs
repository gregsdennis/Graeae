using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenApi.Models;

public class ContactInfo
{
	private static readonly string[] KnownKeys =
	{
		"name",
		"url",
		"email"
	};

	public string? Name { get; set; }
	public Uri? Url { get; set; }
	public string? Email { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public static ContactInfo FromNode(JsonNode? node)
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

	public static JsonNode? ToNode(ContactInfo? contact)
	{
		if (contact == null) return null;

		var obj = new JsonObject();

		obj.MaybeAdd("name", contact.Name);
		obj.MaybeAdd("url", contact.Url?.ToString());
		obj.MaybeAdd("email", contact.Email);
		obj.AddExtensions(contact.ExtensionData);

		return obj;
	}
}
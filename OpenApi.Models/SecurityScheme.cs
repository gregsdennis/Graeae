using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenApi.Models;

public class SecurityScheme
{
	private static readonly string[] KnownKeys =
	{
		"type",
		"description",
		"name",
		"in",
		"scheme",
		"bearerFormat",
		"flows",
		"openIdConnectUrl",
	};

	public SecuritySchemeType Type { get; set; }
	public string? Description { get; set; }
	public string? Name { get; set; }
	public SecuritySchemeLocation In { get; set; }
	public string? Scheme { get; set; }
	public string? BearerFormat { get; set; }
	public OAuthFlowCollection? Flows { get; set; }
	public Uri? OpenIdConnectUrl { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public static SecurityScheme FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		if (obj.ContainsKey("$ref"))
		{
			var scheme = new SecuritySchemeRef(obj.ExpectUri("$ref", "reference"))
			{
				Description = obj.MaybeString("description", "reference"),
				Summary = obj.MaybeString("summary", "reference")
			};

			obj.ValidateReferenceKeys();

			return scheme;
		}
		else
		{
			var scheme = new SecurityScheme
			{
				Type = obj.ExpectEnum<SecuritySchemeType>("type", "securityScheme"),
				Description = obj.MaybeString("description", "response"),
				Name = obj.ExpectString("name", "securityScheme"),
				In = obj.ExpectEnum<SecuritySchemeLocation>("in", "securityScheme"),
				Scheme = obj.ExpectString("scheme", "securityScheme"),
				BearerFormat = obj.MaybeString("bearerFormat", "securityScheme"),
				Flows = obj.TryGetPropertyValue("flows", out var v) ? OAuthFlowCollection.FromNode(v) : null,
				OpenIdConnectUrl = obj.ExpectUri("openIdConnectUrl", "securityScheme"),
				ExtensionData = ExtensionData.FromNode(obj)
			};

			obj.ValidateNoExtraKeys(KnownKeys, scheme.ExtensionData?.Keys);

			return scheme;
		}
	}
}

public class SecuritySchemeRef : SecurityScheme
{
	public Uri Ref { get; }
	public string? Summary { get; set; }
	public new string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public SecuritySchemeRef(Uri reference)
	{
		Ref = reference ?? throw new ArgumentNullException(nameof(reference));
	}

	public void Resolve()
	{
		// resolve the $ref and set all of the props
		// remember to use base.Description

		IsResolved = true;
	}
}
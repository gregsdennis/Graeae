using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OpenApi.Models;

/// <summary>
/// Models a security scheme.
/// </summary>
[JsonConverter(typeof(SecuritySchemeJsonConverter))]
public class SecurityScheme : IRefTargetContainer
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

	public SecuritySchemeType Type { get; private protected set; }
	public string? Description { get; set; }
	public string? Name { get; set; }
	public SecuritySchemeLocation? In { get; set; }
	public string? Scheme { get; set; }
	public string? BearerFormat { get; set; }
	public OAuthFlowCollection? Flows { get; set; }
	public Uri? OpenIdConnectUrl { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	public SecurityScheme(SecuritySchemeType type)
	{
		Type = type;
	}
	private protected SecurityScheme(){}

	public static SecurityScheme FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		SecurityScheme scheme;
		if (obj.ContainsKey("$ref"))
		{
			scheme = new SecuritySchemeRef(obj.ExpectUri("$ref", "reference"))
			{
				Description = obj.MaybeString("description", "reference"),
				Summary = obj.MaybeString("summary", "reference")
			};

			obj.ValidateReferenceKeys();
		}
		else
		{
			scheme = new SecurityScheme(obj.ExpectEnum<SecuritySchemeType>("type", "securityScheme"));
			scheme.Import(obj);

			obj.ValidateNoExtraKeys(KnownKeys, scheme.ExtensionData?.Keys);
		}
		return scheme;
	}

	private protected void Import(JsonObject obj)
	{
		Description = obj.MaybeString("description", "response");
		Name = obj.MaybeString("name", "securityScheme");
		In = obj.MaybeEnum<SecuritySchemeLocation>("in", "securityScheme");
		Scheme = obj.MaybeString("scheme", "securityScheme");
		BearerFormat = obj.MaybeString("bearerFormat", "securityScheme");
		Flows = obj.TryGetPropertyValue("flows", out var v) ? OAuthFlowCollection.FromNode(v) : null;
		OpenIdConnectUrl = obj.MaybeUri("openIdConnectUrl", "securityScheme");
		ExtensionData = ExtensionData.FromNode(obj);
	}

	public static JsonNode? ToNode(SecurityScheme? scheme)
	{
		if (scheme == null) return null;

		var obj = new JsonObject();

		if (scheme is SecuritySchemeRef reference)
		{
			obj.Add("$ref", reference.Ref.ToString());
			obj.MaybeAdd("description", reference.Description);
			obj.MaybeAdd("summary", reference.Summary);
		}
		else
		{
			obj.MaybeAddEnum<SecuritySchemeType>("type", scheme.Type);
			obj.MaybeAdd("description", scheme.Description);
			obj.MaybeAdd("name", scheme.Name);
			obj.MaybeAddEnum("in", scheme.In);
			obj.MaybeAdd("scheme", scheme.Scheme);
			obj.MaybeAdd("bearerFormat", scheme.BearerFormat);
			obj.MaybeAdd("flows", OAuthFlowCollection.ToNode(scheme.Flows));
			obj.MaybeAdd("openIdConnectUrl", scheme.OpenIdConnectUrl?.ToString());
			obj.AddExtensions(scheme.ExtensionData);
		}

		return obj;
	}

	public object? Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		if (keys[0] == "flows")
		{
			if (keys.Length == 1) return Flows;
			return Flows?.Resolve(keys[1..]);
		}

		return ExtensionData?.Resolve(keys);
	}

	public IEnumerable<IComponentRef> FindRefs()
	{
		if (this is SecuritySchemeRef ssRef)
			yield return ssRef;
	}
}

/// <summary>
/// Models a `$ref` to a security scheme.
/// </summary>
public class SecuritySchemeRef : SecurityScheme, IComponentRef
{
	public Uri Ref { get; }
	public string? Summary { get; set; }
	public new string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public SecuritySchemeRef(Uri reference)
	{
		Ref = reference ?? throw new ArgumentNullException(nameof(reference));
	}

	public async Task Resolve(OpenApiDocument root)
	{
		bool import(JsonNode? node)
		{
			if (node is not JsonObject obj) return false;

			Type = obj.ExpectEnum<SecuritySchemeType>("type", "securityScheme");
			Import(obj);
			return true;
		}

		void copy(SecurityScheme other)
		{
			Type = other.Type;
			base.Description = other.Description;
			Name = other.Name;
			In = other.In;
			Scheme = other.Scheme;
			BearerFormat = other.BearerFormat;
			Flows = other.Flows;
			OpenIdConnectUrl = other.OpenIdConnectUrl;
			ExtensionData = other.ExtensionData;
		}

		IsResolved = await RefHelper.Resolve<SecurityScheme>(root, Ref, import, copy);
	}
}

internal class SecuritySchemeJsonConverter : JsonConverter<SecurityScheme>
{
	public override SecurityScheme Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return SecurityScheme.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, SecurityScheme value, JsonSerializerOptions options)
	{
		var json = SecurityScheme.ToNode(value);

		JsonSerializer.Serialize(writer, json, options);
	}
}
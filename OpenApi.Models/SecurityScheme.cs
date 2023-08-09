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

	/// <summary>
	/// Gets the type of security scheme.
	/// </summary>
	public string Type { get; private protected set; }
	/// <summary>
	/// Gets or sets the description.
	/// </summary>
	public string? Description { get; set; }
	/// <summary>
	/// Gets or sets the name.
	/// </summary>
	public string? Name { get; set; }
	/// <summary>
	/// Gets or sets the location of the API key.
	/// </summary>
	public SecuritySchemeLocation? In { get; set; }
	/// <summary>
	/// Gets or sets the scheme.
	/// </summary>
	public string? Scheme { get; set; }
	/// <summary>
	/// Gets or sets the bearer token format.
	/// </summary>
	public string? BearerFormat { get; set; }
	/// <summary>
	/// Gets or sets the collection of OAuth flows.
	/// </summary>
	public OAuthFlowCollection? Flows { get; set; }
	/// <summary>
	/// Gets the OpenID Connect URL.
	/// </summary>
	public Uri? OpenIdConnectUrl { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	/// <summary>
	/// Creates a new <see cref="SecurityScheme"/>
	/// </summary>
	/// <param name="type">The security scheme type</param>
	public SecurityScheme(string type)
	{
		Type = type;
	}
#pragma warning disable CS8618
	private protected SecurityScheme(){}
#pragma warning restore CS8618

	internal static SecurityScheme FromNode(JsonNode? node)
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
			scheme = new SecurityScheme(obj.ExpectString("type", "securityScheme"));
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

	internal static JsonNode? ToNode(SecurityScheme? scheme)
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
			obj.MaybeAdd("type", scheme.Type);
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

	object? IRefTargetContainer.Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		if (keys[0] == "flows")
		{
			if (keys.Length == 1) return Flows;
			return Flows?.Resolve(keys[1..]);
		}

		return ExtensionData?.Resolve(keys);
	}

	internal IEnumerable<IComponentRef> FindRefs()
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
	/// <summary>
	/// The URI for the reference.
	/// </summary>
	public Uri Ref { get; }

	/// <summary>
	/// Gets or sets the summary.
	/// </summary>
	public string? Summary { get; set; }

	/// <summary>
	/// Gets or sets the description.
	/// </summary>
	public new string? Description { get; set; }

	/// <summary>
	/// Gets whether the reference has been resolved.
	/// </summary>
	public bool IsResolved { get; private set; }

	/// <summary>
	/// Creates a new <see cref="SecuritySchemeRef"/>
	/// </summary>
	/// <param name="reference">The reference URI</param>
	public SecuritySchemeRef(Uri reference)
	{
		Ref = reference ?? throw new ArgumentNullException(nameof(reference));
	}

	/// <summary>
	/// Creates a new <see cref="SecuritySchemeRef"/>
	/// </summary>
	/// <param name="reference">The reference URI</param>
	public SecuritySchemeRef(string reference)
	{
		Ref = new Uri(reference ?? throw new ArgumentNullException(nameof(reference)), UriKind.RelativeOrAbsolute);
	}

	async Task IComponentRef.Resolve(OpenApiDocument root)
	{
		bool import(JsonNode? node)
		{
			if (node is not JsonObject obj) return false;

			Type = obj.ExpectString("type", "securityScheme");
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

		IsResolved = await Models.Ref.Resolve<SecurityScheme>(root, Ref, import, copy);
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
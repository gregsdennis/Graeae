namespace OpenApi.Models;

public class SecurityScheme // need to break out into subclasses
{
	public SecuritySchemeType Type { get; private protected set; }
	public string? Description { get; set; }
	public string Name { get; private protected set; }
	public SecuritySchemeLocation In { get; private protected set; }
	public string Scheme { get; private protected set; }
	public string? BearerFormat { get; set; }
	public OAuthFlowCollection Flows { get; private protected set; }
	public Uri OpenIdConnectUrl { get; private protected set; }
	public ExtensionData? ExtensionData { get; set; }

	public SecurityScheme(SecuritySchemeType type, string name, SecuritySchemeLocation location, string scheme, OAuthFlowCollection flows, Uri openIdConnectUrl)
	{
		if (type == SecuritySchemeType.Unspecified)
			throw new ArgumentException("Type cannot be unspecified");
		Type = type;
		Name = name ?? throw new ArgumentNullException(nameof(name));
		if (location == SecuritySchemeLocation.Unspecified)
			throw new ArgumentException("In cannot be unspecified");
		In = location;
		Scheme = scheme ?? throw new ArgumentNullException(nameof(scheme));
		Flows = flows ?? throw new ArgumentNullException(nameof(flows));
		OpenIdConnectUrl = openIdConnectUrl ?? throw new ArgumentNullException(nameof(openIdConnectUrl));
	}
#pragma warning disable CS8618
	internal SecurityScheme(){}
#pragma warning restore CS8618
}

public class SecuritySchemeRef : SecurityScheme
{
	public Uri Ref { get; }
	public string? Summary { get; set; }
	public new string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public SecuritySchemeRef(Uri refUri)
	{
		Ref = refUri ?? throw new ArgumentNullException(nameof(refUri));
	}

	public void Resolve()
	{
		// resolve the $ref and set all of the props
		// remember to use base.Description

		IsResolved = true;
	}
}
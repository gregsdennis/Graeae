namespace OpenApi.Models;

public class Response
{
	public string Description { get; private protected set; }
	public Dictionary<string, Header>? Headers { get; set; }
	public Dictionary<string, MediaType>? Content { get; set; }
	public Dictionary<string, Link>? Links { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public Response(string description)
	{
		Description = description ?? throw new ArgumentNullException(nameof(description));
	}
#pragma warning disable CS8618
	internal Response(){}
#pragma warning restore CS8618
}

public class ResponseRef : Response
{
	public Uri Ref { get; }
	public string? Summary { get; set; }
	public new string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public ResponseRef(Uri refUri)
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
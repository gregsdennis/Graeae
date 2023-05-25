namespace OpenApi.Models;

public class RequestBody
{
	public string? Description { get; set; }
	public Dictionary<string, MediaType> Content { get; private protected set; }
	public bool? Required { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public RequestBody(Dictionary<string, MediaType> content)
	{
		Content = content ?? throw new ArgumentNullException(nameof(content));
	}
#pragma warning disable CS8618
	internal RequestBody(){}
#pragma warning restore CS8618
}

public class RequestBodyRef : RequestBody
{
	public Uri Ref { get; }
	public string? Summary { get; set; }
	public new string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public RequestBodyRef(Uri refUri)
	{
		Ref = refUri ?? throw new ArgumentNullException(nameof(refUri));
	}

	public void Resolve()
	{
		// resolve the $ref and set all of the props
		// remember to use base.*

		IsResolved = true;
	}
}
namespace OpenApi.Models;

public class PathItem
{
	public string? Ref { get; set; } // $ref, probably a uri
	public string? Summary { get; set; }
	public string? Description { get; set; }
	public Operation? Get { get; set; }
	public Operation? Put { get; set; }
	public Operation? Post { get; set; }
	public Operation? Delete { get; set; }
	public Operation? Options { get; set; }
	public Operation? Head { get; set; }
	public Operation? Patch { get; set; }
	public Operation? Trace { get; set; }
	public IEnumerable<Server>? Servers { get; set; }
	public IEnumerable<Parameter>? Parameters { get; set; }
	public ExtensionData? ExtensionData { get; set; }
}

public class PathItemRef : PathItem
{
	public new Uri Ref { get; }
	public new string? Summary { get; set; }
	public new string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public PathItemRef(Uri refUri)
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
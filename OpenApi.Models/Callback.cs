namespace OpenApi.Models;

public class Callback : Dictionary<CallbackExpression, PathItem>
{
	public ExtensionData? ExtensionData { get; set; }
}

public class CallbackRef : Callback
{
	public Uri Ref { get; }
	public string? Summary { get; set; }
	public string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public CallbackRef(Uri refUri)
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
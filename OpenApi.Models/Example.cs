using System.Text.Json.Nodes;

namespace OpenApi.Models;

public class Example
{
	public string? Summary { get; set; }
	public string? Description { get; set; }
	public JsonNode? Value { get; set; } // use JsonNull
	public string? ExternalValue { get; set; }
	public ExtensionData? ExtensionData { get; set; }
}

public class ExampleRef : Example
{
	public Uri Ref { get; }
	public new string? Summary { get; set; }
	public new string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public ExampleRef(Uri refUri)
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
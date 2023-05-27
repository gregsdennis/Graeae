using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenApi.Models;

public class PathItem
{
	private static readonly string[] KnownKeys =
	{
		"summary",
		"description",
		"get",
		"put",
		"post",
		"delete",
		"options",
		"head",
		"patch",
		"trace",
		"servers",
		"parameters"
	};

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

	public static PathItem FromNode(JsonNode? node, JsonSerializerOptions? options)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		if (obj.ContainsKey("$ref"))
		{
			var item = new PathItemRef(obj.ExpectUri("$ref", "reference"))
			{
				Description = obj.MaybeString("description", "reference"),
				Summary = obj.MaybeString("summary", "reference")
			};

			obj.ValidateReferenceKeys();

			return item;
		}
		else
		{
			var item = new PathItem
			{
				Summary = obj.MaybeString("summary", "pathItem"),
				Description = obj.MaybeString("description", "pathItem"),
				Get = obj.TryGetPropertyValue("get", out var get) ? Operation.FromNode(get, options) : null,
				Put = obj.TryGetPropertyValue("put", out var put) ? Operation.FromNode(put, options) : null,
				Post = obj.TryGetPropertyValue("post", out var post) ? Operation.FromNode(post, options) : null,
				Delete = obj.TryGetPropertyValue("delete", out var delete) ? Operation.FromNode(delete, options) : null,
				Options = obj.TryGetPropertyValue("options", out var option) ? Operation.FromNode(option, options) : null,
				Head = obj.TryGetPropertyValue("head", out var head) ? Operation.FromNode(head, options) : null,
				Patch = obj.TryGetPropertyValue("patch", out var patch) ? Operation.FromNode(patch, options) : null,
				Trace = obj.TryGetPropertyValue("trace", out var trace) ? Operation.FromNode(trace, options) : null,
				Servers = obj.MaybeArray("servers", x => Server.FromNode(x)),
				Parameters = obj.MaybeArray("parameters", x => Parameter.FromNode(x, options)),
				ExtensionData = ExtensionData.FromNode(obj)
			};

			obj.ValidateNoExtraKeys(KnownKeys, item.ExtensionData?.Keys);

			return item;
		}
	}
}

public class PathItemRef : PathItem
{
	public Uri Ref { get; }
	public new string? Summary { get; set; }
	public new string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public PathItemRef(Uri reference)
	{
		Ref = reference ?? throw new ArgumentNullException(nameof(reference));
	}

	public void Resolve()
	{
		// resolve the $ref and set all of the props
		// remember to use base.*

		IsResolved = true;
	}
}
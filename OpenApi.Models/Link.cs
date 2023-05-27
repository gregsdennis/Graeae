using System.Text.Json.Nodes;
using System.Text.Json;

namespace OpenApi.Models;

public class Link
{
	private static readonly string[] KnownKeys =
	{
		"operationRef",
		"operationId",
		"parameters",
		"requestBody",
		"description",
		"server"
	};

	public Uri? OperationRef { get; set; }
	public string? OperationId { get; set; }
	public Dictionary<string, RuntimeExpression>? Parameters { get; set; } // can be JsonNode?
	public RuntimeExpression? RequestBody { get; set; } // can be JsonNode?
	public string? Description { get; set; }
	public Server? Server { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public static Link FromNode(JsonNode? node, JsonSerializerOptions? options)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		if (obj.ContainsKey("$ref"))
		{
			var link = new LinkRef(obj.ExpectUri("$ref", "reference"))
			{
				Description = obj.MaybeString("description", "reference"),
				Summary = obj.MaybeString("summary", "reference")
			};

			obj.ValidateReferenceKeys();

			return link;
		}
		else
		{
			var link = new Link
			{
				OperationRef = obj.MaybeUri("operationRef", "link"),
				OperationId = obj.MaybeString("operationId", "link"),
				Parameters = obj.MaybeMap("parameters", x => RuntimeExpression.FromNode(x, options)),
				RequestBody = obj.Maybe("requestBody", x => RuntimeExpression.FromNode(x, options)),
				Description = obj.ExpectString("description", "link"),
				Server = obj.Maybe("server", Server.FromNode),
				ExtensionData = ExtensionData.FromNode(obj)
			};

			obj.ValidateNoExtraKeys(KnownKeys, link.ExtensionData?.Keys);

			return link;
		}
	}
}

public class LinkRef : Link
{
	public Uri Ref { get; }
	public string? Summary { get; set; }
	public new string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public LinkRef(Uri reference)
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
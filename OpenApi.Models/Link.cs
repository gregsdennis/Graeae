using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenApi.Models;

[JsonConverter(typeof(LinkJsonConverter))]
public class Link : IRefTargetContainer
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

	public static Link FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		Link link;
		if (obj.ContainsKey("$ref"))
		{
			link = new LinkRef(obj.ExpectUri("$ref", "reference"))
			{
				Description = obj.MaybeString("description", "reference"),
				Summary = obj.MaybeString("summary", "reference")
			};

			obj.ValidateReferenceKeys();
		}
		else
		{
			link = new Link();
			link.Import(obj);

			obj.ValidateNoExtraKeys(KnownKeys, link.ExtensionData?.Keys);
		}
		return link;
	}

	private protected void Import(JsonObject obj)
	{
		OperationRef = obj.MaybeUri("operationRef", "link");
		OperationId = obj.MaybeString("operationId", "link");
		Parameters = obj.MaybeMap("parameters", RuntimeExpression.FromNode);
		RequestBody = obj.Maybe("requestBody", RuntimeExpression.FromNode);
		Description = obj.MaybeString("description", "link");
		Server = obj.Maybe("server", Server.FromNode);
		ExtensionData = ExtensionData.FromNode(obj);
	}

	public static JsonNode? ToNode(Link? link)
	{
		if (link == null) return null;

		var obj = new JsonObject();

		if (link is LinkRef reference)
		{
			obj.Add("$ref", reference.Ref.ToString());
			obj.MaybeAdd("description", reference.Description);
			obj.MaybeAdd("summary", reference.Summary);
		}
		else
		{
			obj.MaybeAdd("operationRef", link.OperationRef?.ToString());
			obj.MaybeAdd("operationId", link.OperationId);
			obj.MaybeAddMap("parameters", link.Parameters, x => x.ToString());
			obj.MaybeAdd("requestBody", link.RequestBody?.ToString());
			obj.MaybeAdd("description", link.Description);
			obj.MaybeAdd("server", Server.ToNode(link.Server));
			obj.AddExtensions(link.ExtensionData);
		}

		return obj;
	}

	public object? Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		if (keys[0] == "server")
		{
			if (keys.Length == 1) return Server;
			return Server?.Resolve(keys[1..]);
		}

		return ExtensionData?.Resolve(keys);
	}

	public IEnumerable<IComponentRef> FindRefs()
	{
		if (this is LinkRef lRef)
			yield return lRef;
	}
}

public class LinkRef : Link, IComponentRef
{
	public Uri Ref { get; }
	public string? Summary { get; set; }
	public new string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public LinkRef(Uri reference)
	{
		Ref = reference ?? throw new ArgumentNullException(nameof(reference));
	}

	public void Resolve(OpenApiDocument root)
	{
		bool import(JsonNode? node)
		{
			if (node is not JsonObject obj) return false;

			Import(obj);
			return true;
		}

		void copy(Link other)
		{
			OperationRef = other.OperationRef;
			OperationId = other.OperationId;
			Parameters = other.Parameters;
			RequestBody = other.RequestBody;
			base.Description = other.Description;
			Server = other.Server;
			ExtensionData = other.ExtensionData;
		}

		IsResolved = RefHelper.Resolve<Link>(root, Ref, import, copy);
	}
}

public class LinkJsonConverter : JsonConverter<Link>
{
	public override Link? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return Link.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, Link value, JsonSerializerOptions options)
	{
		var json = Link.ToNode(value);

		JsonSerializer.Serialize(writer, json, options);
	}
}

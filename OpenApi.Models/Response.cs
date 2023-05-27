using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OpenApi.Models;

[JsonConverter(typeof(ResponseJsonConverter))]
public class Response
{
	private static readonly string[] KnownKeys =
	{
		"description",
		"headers",
		"content",
		"links"
	};

	public string Description { get; set; } = null!;
	public Dictionary<string, Header>? Headers { get; set; }
	public Dictionary<string, MediaType>? Content { get; set; }
	public Dictionary<string, Link>? Links { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public static Response FromNode(JsonNode? node, JsonSerializerOptions? options)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		if (obj.ContainsKey("$ref"))
		{
			var response = new ResponseRef(obj.ExpectUri("$ref", "reference"))
			{
				Description = obj.MaybeString("description", "reference"),
				Summary = obj.MaybeString("summary", "reference")
			};

			obj.ValidateReferenceKeys();

			return response;
		}
		else
		{
			var response = new Response
			{
				Description = obj.ExpectString("description", "response"),
				Headers = obj.MaybeMap("headers", x => Header.FromNode(x, options)),
				Content = obj.MaybeMap("content", x => MediaType.FromNode(x, options)),
				Links = obj.MaybeMap("links", x => Link.FromNode(x, options))
			};

			obj.ValidateNoExtraKeys(KnownKeys, response.ExtensionData?.Keys);

			return response;
		}
	}
}

public class ResponseRef : Response
{
	public Uri Ref { get; }
	public string? Summary { get; set; }
	public new string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public ResponseRef(Uri reference)
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

public class ResponseJsonConverter : JsonConverter<Response>
{
	public override Response? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return Response.FromNode(obj, options);
	}

	public override void Write(Utf8JsonWriter writer, Response value, JsonSerializerOptions options)
	{
		throw new NotImplementedException();
	}
}
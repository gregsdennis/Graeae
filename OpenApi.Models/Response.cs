using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Schema;

namespace OpenApi.Models;

[JsonConverter(typeof(ResponseJsonConverter))]
public class Response : IRefResolvable
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

	public static JsonNode? ToNode(Response? response, JsonSerializerOptions? options)
	{
		if (response == null) return null;

		var obj = new JsonObject();
		
		if (response is ResponseRef reference)
		{
			obj.Add("$ref", reference.Ref.ToString());
			obj.MaybeAdd("description", reference.Description);
			obj.MaybeAdd("summary", reference.Summary);
		}
		else
		{
			obj.MaybeAdd("description", response.Description);
			obj.MaybeAddMap("headers", response.Headers, x => Header.ToNode(x, options));
			obj.MaybeAddMap("content", response.Content, x => MediaType.ToNode(x, options));
			obj.MaybeAddMap("links", response.Links, x => Link.ToNode(x, options));
			obj.AddExtensions(response.ExtensionData);
		}

		return obj;
	}

	public object? Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		int keysConsumed = 1;
		IRefResolvable? target = null;
		switch (keys[0])
		{
			case "headers":
				if (keys.Length == 1) return null;
				keysConsumed++;
				target = Headers?.GetFromMap(keys[1]);
				break;
			case "content":
				if (keys.Length == 1) return null;
				keysConsumed++;
				target = Content?.GetFromMap(keys[1]);
				break;
			case "links":
				if (keys.Length == 1) return null;
				keysConsumed++;
				target = Links?.GetFromMap(keys[1]);
				break;
		}

		return target != null
			? target.Resolve(keys[keysConsumed..])
			: ExtensionData?.Resolve(keys);
	}

	public IEnumerable<JsonSchema> FindSchemas()
	{
		return GeneralHelpers.Collect(
			Headers?.Values.SelectMany(x => x.FindSchemas()),
			Content?.Values.SelectMany(x => x.FindSchemas())
		);
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
		var json = Response.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}
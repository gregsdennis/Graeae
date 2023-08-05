using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Schema;

namespace OpenApi.Models;

/// <summary>
/// Models a response.
/// </summary>
[JsonConverter(typeof(ResponseJsonConverter))]
public class Response : IRefTargetContainer
{
	private static readonly string[] KnownKeys =
	{
		"description",
		"headers",
		"content",
		"links"
	};

	public string Description { get; private protected set; }
	public Dictionary<string, Header>? Headers { get; set; }
	public Dictionary<string, MediaType>? Content { get; set; }
	public Dictionary<string, Link>? Links { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	public Response(string description)
	{
		Description = description;
	}
#pragma warning disable CS8618
	private protected Response(){}
#pragma warning restore CS8618

	public static Response FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		Response response;
		if (obj.ContainsKey("$ref"))
		{
			response = new ResponseRef(obj.ExpectUri("$ref", "reference"))
			{
				Description = obj.MaybeString("description", "reference"),
				Summary = obj.MaybeString("summary", "reference")
			};

			obj.ValidateReferenceKeys();
		}
		else
		{
			response = new Response(obj.ExpectString("description", "response"));
			response.Import(obj);

			obj.ValidateNoExtraKeys(KnownKeys, response.ExtensionData?.Keys);
		}
		return response;
	}

	private protected void Import(JsonObject obj)
	{
		Headers = obj.MaybeMap("headers", Header.FromNode);
		Content = obj.MaybeMap("content", MediaType.FromNode);
		Links = obj.MaybeMap("links", Link.FromNode);
		ExtensionData = ExtensionData.FromNode(obj);
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
			obj.MaybeAddMap("links", response.Links, x => Link.ToNode(x));
			obj.AddExtensions(response.ExtensionData);
		}

		return obj;
	}

	public object? Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		int keysConsumed = 1;
		IRefTargetContainer? target = null;
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

	public IEnumerable<IComponentRef> FindRefs()
	{
		if (this is ResponseRef rRef)
			yield return rRef;

		var theRest = GeneralHelpers.Collect(
			Headers?.Values.SelectMany(x => x.FindRefs()),
			Content?.Values.SelectMany(x => x.FindRefs()),
			Links?.Values.SelectMany(x => x.FindRefs())
		);

		foreach (var parameter in theRest)
		{
			yield return parameter;
		}
	}
}

/// <summary>
/// Models a `$ref` to a response.
/// </summary>
public class ResponseRef : Response, IComponentRef
{
	public Uri Ref { get; }
	public string? Summary { get; set; }
	public new string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public ResponseRef(Uri reference)
	{
		Ref = reference ?? throw new ArgumentNullException(nameof(reference));
	}

	public async Task Resolve(OpenApiDocument root)
	{
		bool import(JsonNode? node)
		{
			if (node is not JsonObject obj) return false;

			base.Description = obj.ExpectString("description", "response");
			Import(obj);
			return true;
		}

		void copy(Response other)
		{
			base.Description = other.Description;
			Headers = other.Headers;
			Content = other.Content;
			Links = other.Links;
			ExtensionData = other.ExtensionData;
		}

		IsResolved = await RefHelper.Resolve<Response>(root, Ref, import, copy);
	}
}

internal class ResponseJsonConverter : JsonConverter<Response>
{
	public override Response Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return Response.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, Response value, JsonSerializerOptions options)
	{
		var json = Response.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}
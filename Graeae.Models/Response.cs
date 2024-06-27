using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Schema;

namespace Graeae.Models;

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

	/// <summary>
	/// Gets the description.
	/// </summary>
	public string Description { get; private protected set; }
	/// <summary>
	/// Gets or sets the header collection.
	/// </summary>
	public Dictionary<string, Header>? Headers { get; set; }
	/// <summary>
	/// Gets or sets the content collection.
	/// </summary>
	public Dictionary<string, MediaType>? Content { get; set; }
	/// <summary>
	/// Gets or sets the link collection.
	/// </summary>
	public Dictionary<string, Link>? Links { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	/// <summary>
	/// Creates a new <see cref="Response"/>
	/// </summary>
	/// <param name="description">The description</param>
	public Response(string description)
	{
		Description = description;
	}
#pragma warning disable CS8618
	private protected Response(){}
#pragma warning restore CS8618

	internal static Response FromNode(JsonNode? node)
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

	internal static JsonNode? ToNode(Response? response, JsonSerializerOptions? options)
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
			obj.MaybeAddMap("links", response.Links, Link.ToNode);
			obj.AddExtensions(response.ExtensionData);
		}

		return obj;
	}

	object? IRefTargetContainer.Resolve(ReadOnlySpan<string> keys)
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

	internal IEnumerable<JsonSchema> FindSchemas()
	{
		return GeneralHelpers.Collect(
			Headers?.Values.SelectMany(x => x.FindSchemas()),
			Content?.Values.SelectMany(x => x.FindSchemas())
		);
	}

	internal IEnumerable<IComponentRef> FindRefs()
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
	/// <summary>
	/// The URI for the reference.
	/// </summary>
	public Uri Ref { get; }

	/// <summary>
	/// Gets or sets the summary.
	/// </summary>
	public string? Summary { get; set; }

	/// <summary>
	/// Gets or sets the description.
	/// </summary>
	public new string? Description { get; set; }

	/// <summary>
	/// Gets whether the reference has been resolved.
	/// </summary>
	public bool IsResolved { get; private set; }

	/// <summary>
	/// Creates a new <see cref="ResponseRef"/>
	/// </summary>
	/// <param name="reference">The reference URI</param>
	public ResponseRef(Uri reference)
	{
		Ref = reference ?? throw new ArgumentNullException(nameof(reference));
	}

	/// <summary>
	/// Creates a new <see cref="ResponseRef"/>
	/// </summary>
	/// <param name="reference">The reference URI</param>
	public ResponseRef(string reference)
	{
		Ref = new Uri(reference ?? throw new ArgumentNullException(nameof(reference)), UriKind.RelativeOrAbsolute);
	}

	async Task IComponentRef.Resolve(OpenApiDocument root)
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

		IsResolved = await Models.Ref.Resolve<Response>(root, Ref, import, copy);
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
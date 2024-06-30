using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Schema;

namespace Graeae.Models;

/// <summary>
/// Models a request body.
/// </summary>
[JsonConverter(typeof(RequestBodyJsonConverter))]
public class RequestBody : IRefTargetContainer
{
	private static readonly string[] KnownKeys =
	{
		"description",
		"content",
		"required"
	};

	/// <summary>
	/// Gets or sets the description.
	/// </summary>
	public string? Description { get; set; }
	/// <summary>
	/// Gets the content collection.
	/// </summary>
	public Dictionary<string, MediaType> Content { get; private protected set; }
	/// <summary>
	/// Gets or sets whether the request body is required.
	/// </summary>
	public bool? Required { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	/// <summary>
	/// Creates a new <see cref="RequestBody"/>
	/// </summary>
	/// <param name="content"></param>
	public RequestBody(Dictionary<string, MediaType> content)
	{
		Content = content;
	}
#pragma warning disable CS8618
	private protected RequestBody(){}
#pragma warning restore CS8618

	internal static RequestBody FromNode(JsonNode? node, JsonSerializerOptions? options)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		RequestBody body;
		if (obj.ContainsKey("$ref"))
		{
			body = new RequestBodyRef(obj.ExpectUri("$ref", "reference"))
			{
				Description = obj.MaybeString("description", "reference"),
				Summary = obj.MaybeString("summary", "reference")
			};

			obj.ValidateReferenceKeys();
		}
		else
		{
			body = new RequestBody(obj.ExpectMap("content", "request body", x => MediaType.FromNode(x, options)));
			body.Import(obj);

			obj.ValidateNoExtraKeys(KnownKeys, body.ExtensionData?.Keys);
		}
		return body;
	}

	private protected void Import(JsonObject obj)
	{
		Description = obj.MaybeString("description", "request body");
		Required = obj.MaybeBool("required", "request body");
		ExtensionData = ExtensionData.FromNode(obj);
	}

	internal static JsonNode? ToNode(RequestBody? body, JsonSerializerOptions? options)
	{
		if (body == null) return null;

		var obj = new JsonObject();

		if (body is RequestBodyRef reference)
		{
			obj.Add("$ref", reference.Ref.ToString());
			obj.MaybeAdd("description", reference.Description);
			obj.MaybeAdd("summary", reference.Summary);
		}
		else
		{
			obj.MaybeAdd("description", body.Description);
			obj.MaybeAddMap("content", body.Content, x => MediaType.ToNode(x, options));
			obj.MaybeAdd("required", body.Required);
			obj.AddExtensions(body.ExtensionData);
		}

		return obj;
	}

	object? IRefTargetContainer.Resolve(ReadOnlySpan<string> keys)
	{
		if (keys.Length == 0) return this;

		if (keys[0] == "server")
		{
			if (keys.Length == 1) return null;
			var target = Content.GetFromMap(keys[1]);
			return target?.Resolve(keys[2..]);
		}

		return ExtensionData?.Resolve(keys);
	}

	internal IEnumerable<JsonSchema> FindSchemas()
	{
		return Content.Values.SelectMany(x => x.FindSchemas());
	}

	internal IEnumerable<IComponentRef> FindRefs()
	{
		if (this is RequestBodyRef rbRef)
			yield return rbRef;

		var theRest = Content.Values.SelectMany(x => x.FindRefs());

		foreach (var reference in theRest)
		{
			yield return reference;
		}
	}
}

/// <summary>
/// Models a `$ref` to a request body.
/// </summary>
public class RequestBodyRef : RequestBody, IComponentRef
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
	/// Creates a new <see cref="RequestBodyRef"/>
	/// </summary>
	/// <param name="reference">The reference URI</param>
	public RequestBodyRef(Uri reference)
	{
		Ref = reference ?? throw new ArgumentNullException(nameof(reference));
	}

	/// <summary>
	/// Creates a new <see cref="RequestBodyRef"/>
	/// </summary>
	/// <param name="reference">The reference URI</param>
	public RequestBodyRef(string reference)
	{
		Ref = new Uri(reference ?? throw new ArgumentNullException(nameof(reference)), UriKind.RelativeOrAbsolute);
	}

	async Task IComponentRef.Resolve(OpenApiDocument root, JsonSerializerOptions? options)
	{
		bool import(JsonNode? node)
		{
			if (node is not JsonObject obj) return false;

			Content = obj.ExpectMap("content", "request body", x => MediaType.FromNode(x, options));
			Import(obj);
			return true;
		}

		void copy(RequestBody other)
		{
			Content = other.Content;
			base.Description = other.Description;
			Required = other.Required;
			ExtensionData = other.ExtensionData;
		}

		IsResolved = await Models.Ref.Resolve<RequestBody>(root, Ref, import, copy);
	}
}

internal class RequestBodyJsonConverter : JsonConverter<RequestBody>
{
	public override RequestBody Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return RequestBody.FromNode(obj, options);
	}

	public override void Write(Utf8JsonWriter writer, RequestBody value, JsonSerializerOptions options)
	{
		var json = RequestBody.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}
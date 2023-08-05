using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Schema;

namespace OpenApi.Models;

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

	public string? Description { get; set; }
	public Dictionary<string, MediaType> Content { get; private protected set; }
	public bool? Required { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	public RequestBody(Dictionary<string, MediaType> content)
	{
		Content = content;
	}
#pragma warning disable CS8618
	private protected RequestBody(){}
#pragma warning restore CS8618

	public static RequestBody FromNode(JsonNode? node)
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
			body = new RequestBody(obj.ExpectMap("content", "request body", MediaType.FromNode));
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

	public static JsonNode? ToNode(RequestBody? body, JsonSerializerOptions? options)
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

	public object? Resolve(Span<string> keys)
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

	public IEnumerable<JsonSchema> FindSchemas()
	{
		return Content.Values.SelectMany(x => x.FindSchemas());
	}

	public IEnumerable<IComponentRef> FindRefs()
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
	public Uri Ref { get; }
	public string? Summary { get; set; }
	public new string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public RequestBodyRef(Uri reference)
	{
		Ref = reference ?? throw new ArgumentNullException(nameof(reference));
	}

	public async Task Resolve(OpenApiDocument root)
	{
		bool import(JsonNode? node)
		{
			if (node is not JsonObject obj) return false;

			Content = obj.ExpectMap("content", "request body", MediaType.FromNode);
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

		IsResolved = await RefHelper.Resolve<RequestBody>(root, Ref, import, copy);
	}
}

internal class RequestBodyJsonConverter : JsonConverter<RequestBody>
{
	public override RequestBody Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return RequestBody.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, RequestBody value, JsonSerializerOptions options)
	{
		var json = RequestBody.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}
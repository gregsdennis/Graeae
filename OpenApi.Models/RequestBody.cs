using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Schema;

namespace OpenApi.Models;

[JsonConverter(typeof(RequestBodyJsonConverter))]
public class RequestBody : IRefResolvable
{
	private static readonly string[] KnownKeys =
	{
		"description",
		"content",
		"required"
	};

	public string? Description { get; set; }
	public Dictionary<string, MediaType> Content { get; set; }
	public bool? Required { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public static RequestBody FromNode(JsonNode? node, JsonSerializerOptions? options)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		if (obj.ContainsKey("$ref"))
		{
			var link = new RequestBodyRef(obj.ExpectUri("$ref", "reference"))
			{
				Description = obj.MaybeString("description", "reference"),
				Summary = obj.MaybeString("summary", "reference")
			};

			obj.ValidateReferenceKeys();

			return link;
		}
		else
		{
			var link = new RequestBody
			{
				Description = obj.MaybeString("description", "request body"),
				Content = obj.ExpectMap("content", "request body", x => MediaType.FromNode(x, options)),
				Required = obj.MaybeBool("required", "request body"),
				ExtensionData = ExtensionData.FromNode(obj)
			};

			obj.ValidateNoExtraKeys(KnownKeys, link.ExtensionData?.Keys);

			return link;
		}
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
}

public class RequestBodyRef : RequestBody
{
	public Uri Ref { get; }
	public string? Summary { get; set; }
	public new string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public RequestBodyRef(Uri reference)
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

public class RequestBodyJsonConverter : JsonConverter<RequestBody>
{
	public override RequestBody? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
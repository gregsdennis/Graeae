using System.Text.Json;
using System.Text.Json.Nodes;
using Json.More;
using Json.Schema;

namespace OpenApi.Models;

public class Parameter
{
	private static readonly string[] KnownKeys =
	{
		"description",
		"required",
		"deprecated",
		"allowEmptyValue",
		"style",
		"explode",
		"allowReserved",
		"schema",
		"example",
		"examples",
		"content"
	};

	public string Name { get; set; }
	public ParameterLocation In { get; set; }
	public string? Description { get; set; }
	public bool? Required { get; set; }
	public bool? Deprecated { get; set; }
	public bool? AllowEmptyValue { get; set; }
	public ParameterStyle? Style { get; set; }
	public bool? Explode { get; set; }
	public bool? AllowReserved { get; set; }
	public JsonSchema? Schema { get; set; }
	public JsonNode? Example { get; set; } // use JsonNull
	public Dictionary<string, Example>? Examples { get; set; }
	public Dictionary<string, MediaType>? Content { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public static Parameter FromNode(JsonNode? node, JsonSerializerOptions? options)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		if (obj.ContainsKey("$ref"))
		{
			var response = new ParameterRef(obj.ExpectUri("$ref", "reference"))
			{
				Description = obj.MaybeString("description", "reference"),
				Summary = obj.MaybeString("summary", "reference")
			};

			obj.ValidateReferenceKeys();

			return response;
		}
		else
		{
			var response = new Parameter
			{
				Description = obj.ExpectString("description", "header"),
				Required = obj.MaybeBool("required", "header"),
				Deprecated = obj.MaybeBool("deprecated", "header"),
				AllowEmptyValue = obj.MaybeBool("allowEmptyValue", "header"),
				Style = obj.MaybeEnum<ParameterStyle>("style", "header"),
				Explode = obj.MaybeBool("explode", "header"),
				AllowReserved = obj.MaybeBool("allowReserved", "header"),
				Schema = obj.MaybeDeserialize<JsonSchema>("schema", options),
				Example = obj.TryGetPropertyValue("example", out var v) ? v ?? JsonNull.SignalNode : null,
				Examples = obj.MaybeMap("examples", Models.Example.FromNode),
				Content = obj.MaybeMap("content", x => MediaType.FromNode(x, options)),
				ExtensionData = ExtensionData.FromNode(obj)
			};

			obj.ValidateNoExtraKeys(KnownKeys, response.ExtensionData?.Keys);

			return response;
		}
	}
}

public class ParameterRef : Parameter
{
	public Uri Ref { get; }
	public string? Summary { get; set; }
	public new string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public ParameterRef(Uri reference)
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
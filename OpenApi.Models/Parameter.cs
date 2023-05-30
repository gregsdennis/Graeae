using System.Text.Json;
using System.Text.Json.Nodes;
using Json.More;
using Json.Schema;

namespace OpenApi.Models;

public class Parameter
{
	private static readonly string[] KnownKeys =
	{
		"name",
		"in",
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
				Name = obj.ExpectString("name", "parameter"),
				In = obj.ExpectEnum<ParameterLocation>("in", "parameter"),
				Description = obj.MaybeString("description", "parameter"),
				Required = obj.MaybeBool("required", "parameter"),
				Deprecated = obj.MaybeBool("deprecated", "parameter"),
				AllowEmptyValue = obj.MaybeBool("allowEmptyValue", "parameter"),
				Style = obj.MaybeEnum<ParameterStyle>("style", "parameter"),
				Explode = obj.MaybeBool("explode", "parameter"),
				AllowReserved = obj.MaybeBool("allowReserved", "parameter"),
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

	public static JsonNode? ToNode(Parameter? parameter, JsonSerializerOptions? options)
	{
		if (parameter == null) return null;

		var obj = new JsonObject();

		if (parameter is ParameterRef reference)
		{
			obj.Add("$ref", reference.Ref.ToString());
			obj.MaybeAdd("description", reference.Description);
			obj.MaybeAdd("summary", reference.Summary);
		}
		else
		{
			obj.Add("name", parameter.Name);
			obj.MaybeAddEnum<ParameterLocation>("in", parameter.In);
			obj.MaybeAdd("description", parameter.Description);
			obj.MaybeAdd("required", parameter.Required);
			obj.MaybeAdd("deprecated", parameter.Deprecated);
			obj.MaybeAdd("allowEmptyValue", parameter.AllowEmptyValue);
			obj.MaybeAddEnum("style", parameter.Style);
			obj.MaybeAdd("explode", parameter.Explode);
			obj.MaybeAdd("allowReserved", parameter.AllowReserved);
			obj.MaybeSerialize("schema", parameter.Schema, options);
			obj.MaybeAdd("example", parameter.Example.Copy());
			obj.MaybeAddMap("examples", parameter.Examples, x => Models.Example.ToNode(x, options));
			obj.MaybeAddMap("content", parameter.Content, x => MediaType.ToNode(x, options));
			obj.AddExtensions(parameter.ExtensionData);
		}

		return obj;
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
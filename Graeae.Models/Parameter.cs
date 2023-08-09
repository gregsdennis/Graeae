using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.More;
using Json.Schema;

namespace Graeae.Models;

/// <summary>
/// Models a parameter.
/// </summary>
[JsonConverter(typeof(ParameterJsonConverter))]
public class Parameter : IRefTargetContainer
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

	/// <summary>
	/// Gets the name.
	/// </summary>
	public string Name { get; private protected set; }
	/// <summary>
	/// Gets the parameter location.
	/// </summary>
	public ParameterLocation In { get; private protected set; }
	/// <summary>
	/// Gets or sets the description.
	/// </summary>
	public string? Description { get; set; }
	/// <summary>
	/// Gets or sets whether the parameter is required.
	/// </summary>
	public bool? Required { get; set; }
	/// <summary>
	/// Gets or sets whether the parameter is deprecated.
	/// </summary>
	public bool? Deprecated { get; set; }
	/// <summary>
	/// Gets or sets whether the parameter is allowed to be present with an empty value.
	/// </summary>
	public bool? AllowEmptyValue { get; set; }
	/// <summary>
	/// Gets or sets how the parameter value will be serialized.
	/// </summary>
	public ParameterStyle? Style { get; set; }
	/// <summary>
	/// Gets or sets whether this will be exploded into multiple parameters.
	/// </summary>
	public bool? Explode { get; set; }
	/// <summary>
	/// Gets or sets whether the parameter value should allow reserved characters.
	/// </summary>
	public bool? AllowReserved { get; set; }
	/// <summary>
	/// Gets or sets a schema for the content.
	/// </summary>
	public JsonSchema? Schema { get; set; }
	/// <summary>
	/// Gets or sets an example.
	/// </summary>
	public JsonNode? Example { get; set; }
	/// <summary>
	/// Gets or sets a collection of examples.
	/// </summary>
	public Dictionary<string, Example>? Examples { get; set; }
	/// <summary>
	/// Gets or sets a collection of content.
	/// </summary>
	public Dictionary<string, MediaType>? Content { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	/// <summary>
	/// Creates a new <see cref="Parameter"/>
	/// </summary>
	/// <param name="name">The name</param>
	/// <param name="in">The parameter location</param>
	public Parameter(string name, ParameterLocation @in)
	{
		Name = name;
		In = @in;
	}
#pragma warning disable CS8618
	private protected Parameter(){}
#pragma warning restore CS8618

	internal static Parameter FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		Parameter response;
		if (obj.ContainsKey("$ref"))
		{
			response = new ParameterRef(obj.ExpectUri("$ref", "reference"))
			{
				Description = obj.MaybeString("description", "reference"),
				Summary = obj.MaybeString("summary", "reference")
			};

			obj.ValidateReferenceKeys();
		}
		else
		{
			response = new Parameter(
				obj.ExpectString("name", "parameter"),
				obj.ExpectEnum<ParameterLocation>("in", "parameter"));
			response.Import(obj);

			obj.ValidateNoExtraKeys(KnownKeys, response.ExtensionData?.Keys);
		}
		return response;
	}

	private protected void Import(JsonObject obj)
	{
		Description = obj.MaybeString("description", "parameter");
		Required = obj.MaybeBool("required", "parameter");
		Deprecated = obj.MaybeBool("deprecated", "parameter");
		AllowEmptyValue = obj.MaybeBool("allowEmptyValue", "parameter");
		Style = obj.MaybeEnum<ParameterStyle>("style", "parameter");
		Explode = obj.MaybeBool("explode", "parameter");
		AllowReserved = obj.MaybeBool("allowReserved", "parameter");
		Schema = obj.MaybeDeserialize<JsonSchema>("schema");
		Example = obj.TryGetPropertyValue("example", out var v) ? v ?? JsonNull.SignalNode : null;
		Examples = obj.MaybeMap("examples", Models.Example.FromNode);
		Content = obj.MaybeMap("content", MediaType.FromNode);
		ExtensionData = ExtensionData.FromNode(obj);
	}

	internal static JsonNode? ToNode(Parameter? parameter, JsonSerializerOptions? options)
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
			obj.MaybeAddMap("examples", parameter.Examples, Models.Example.ToNode);
			obj.MaybeAddMap("content", parameter.Content, x => MediaType.ToNode(x, options));
			obj.AddExtensions(parameter.ExtensionData);
		}

		return obj;
	}

	object? IRefTargetContainer.Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		int keysConsumed = 1;
		IRefTargetContainer? target = null;
		switch (keys[0])
		{
			case "schema":
				if (Schema == null) return null;
				if (keys.Length == 1) return Schema;
				// TODO: consider some other kind of value being buried in a schema
				throw new NotImplementedException();
			case "example":
				return Example?.GetFromNode(keys[1..]);
			case "examples":
				if (keys.Length == 1) return null;
				keysConsumed++;
				target = Examples?.GetFromMap(keys[1]);
				break;
			case "content":
				if (keys.Length == 1) return null;
				keysConsumed++;
				target = Content?.GetFromMap(keys[1]);
				break;
		}

		return target != null
			? target.Resolve(keys[keysConsumed..])
			: ExtensionData?.Resolve(keys);
	}

	internal IEnumerable<JsonSchema> FindSchemas()
	{
		if (Schema != null)
			yield return Schema;

		var theRest = GeneralHelpers.Collect(Content?.Values.SelectMany(x => x.FindSchemas()));

		foreach (var schema in theRest)
		{
			yield return schema;
		}
	}

	internal IEnumerable<IComponentRef> FindRefs()
	{
		if (this is ParameterRef pRef)
			yield return pRef;

		var theRest = GeneralHelpers.Collect(
			Examples?.Values.SelectMany(x => x.FindRefs())
		);

		foreach (var reference in theRest)
		{
			yield return reference;
		}
	}
}

/// <summary>
/// Models a `$ref` to a parameter.
/// </summary>
public class ParameterRef : Parameter, IComponentRef
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
	/// Creates a new <see cref="ParameterRef"/>
	/// </summary>
	/// <param name="reference">The reference URI</param>
	public ParameterRef(Uri reference)
	{
		Ref = reference ?? throw new ArgumentNullException(nameof(reference));
	}

	/// <summary>
	/// Creates a new <see cref="ParameterRef"/>
	/// </summary>
	/// <param name="reference">The reference URI</param>
	public ParameterRef(string reference)
	{
		Ref = new Uri(reference ?? throw new ArgumentNullException(nameof(reference)), UriKind.RelativeOrAbsolute);
	}

	async Task IComponentRef.Resolve(OpenApiDocument root)
	{
		bool import(JsonNode? node)
		{
			if (node is not JsonObject obj) return false;

			Name = obj.ExpectString("name", "parameter");
			In = obj.ExpectEnum<ParameterLocation>("in", "parameter");

			Import(obj);
			return true;
		}

		void copy(Parameter other)
		{
			Name = other.Name;
			In = other.In;
			base.Description = other.Description;
			Required = other.Required;
			Deprecated = other.Deprecated;
			AllowEmptyValue = other.AllowEmptyValue;
			Style = other.Style;
			Explode = other.Explode;
			AllowReserved = other.AllowReserved;
			Schema = other.Schema;
			Example = other.Example;
			Examples = other.Examples;
			Content = other.Content;
			ExtensionData = other.ExtensionData;
		}

		IsResolved = await Models.Ref.Resolve<Parameter>(root, Ref, import, copy);
	}
}

internal class ParameterJsonConverter : JsonConverter<Parameter>
{
	public override Parameter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return Parameter.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, Parameter value, JsonSerializerOptions options)
	{
		var json = Parameter.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}
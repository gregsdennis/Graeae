using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Schema;

namespace Graeae.Models;

/// <summary>
/// Models an operation.
/// </summary>
[JsonConverter(typeof(OperationJsonConverter))]
public class Operation : IRefTargetContainer
{
	private static readonly string[] KnownKeys =
	{
		"tags",
		"summary",
		"description",
		"externalDocs",
		"operationId",
		"parameters",
		"requestBody",
		"responses",
		"callbacks",
		"deprecated",
		"security",
		"servers"
	};

	/// <summary>
	/// Gets or sets the tags.
	/// </summary>
	public IEnumerable<string>? Tags { get; set; }
	/// <summary>
	/// Gets or sets the summary.
	/// </summary>
	public string? Summary { get; set; }
	/// <summary>
	/// Gets or sets the description.
	/// </summary>
	public string? Description { get; set; }
	/// <summary>
	/// Gets or sets external documentation.
	/// </summary>
	public ExternalDocumentation? ExternalDocs { get; set; }
	/// <summary>
	/// Gets or sets the operation ID.
	/// </summary>
	public string? OperationId { get; set; }
	/// <summary>
	/// Gets or sets the parameters.
	/// </summary>
	public IEnumerable<Parameter>? Parameters { get; set; }
	/// <summary>
	/// Gets or sets the request body.
	/// </summary>
	public RequestBody? RequestBody { get; set; }
	/// <summary>
	/// Gets or sets the response collection.
	/// </summary>
	public ResponseCollection? Responses { get; set; }
	/// <summary>
	/// Gets or sets the callbacks collection.
	/// </summary>
	public Dictionary<string, Callback>? Callbacks { get; set; }
	/// <summary>
	/// Gets or sets whether the operation is deprecated.
	/// </summary>
	public bool? Deprecated { get; set; }
	/// <summary>
	/// Gets or sets the security requirements.
	/// </summary>
	public IEnumerable<SecurityRequirement>? Security { get; set; }
	/// <summary>
	/// Gets or sets the server collection.
	/// </summary>
	public IEnumerable<Server>? Servers { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	internal static Operation FromNode(JsonNode? node)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		var operation = new Operation
		{
			Tags = obj.MaybeArray("tags", x => x is JsonValue v && v.TryGetValue(out string? s) ? s : throw new JsonException("tags must be strings")),
			Summary = obj.MaybeString("summary", "operation"),
			Description = obj.MaybeString("description", "operation"),
			ExternalDocs = obj.Maybe("externalDocs", ExternalDocumentation.FromNode),
			OperationId = obj.MaybeString("operationId", "operation"),
			Parameters = obj.MaybeArray("parameters", Parameter.FromNode),
			RequestBody = obj.Maybe("requestBody", RequestBody.FromNode),
			Responses = obj.Maybe("responses", ResponseCollection.FromNode),
			Callbacks = obj.MaybeMap("callbacks", Callback.FromNode),
			Deprecated = obj.MaybeBool("deprecated", "operation"),
			Security = obj.MaybeArray("security", SecurityRequirement.FromNode),
			Servers = obj.MaybeArray("servers", Server.FromNode),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, operation.ExtensionData?.Keys);

		return operation;
	}

	internal static JsonNode? ToNode(Operation? operation, JsonSerializerOptions? options)
	{
		if (operation == null) return null;

		var obj = new JsonObject();

		obj.MaybeAddArray("tags", operation.Tags, x => x);
		obj.MaybeAdd("summary", operation.Summary);
		obj.MaybeAdd("description", operation.Description);
		obj.MaybeAdd("externalDocs", ExternalDocumentation.ToNode(operation.ExternalDocs));
		obj.MaybeAdd("operationId", operation.OperationId);
		obj.MaybeAddArray("parameters", operation.Parameters, x => Parameter.ToNode(x, options));
		obj.MaybeAdd("requestBody", RequestBody.ToNode(operation.RequestBody, options));
		obj.MaybeAdd("responses", ResponseCollection.ToNode(operation.Responses, options));
		obj.MaybeAddMap("callbacks", operation.Callbacks, x => Callback.ToNode(x, options));
		obj.MaybeAdd("deprecated", operation.Deprecated);
		obj.MaybeAddArray("security", operation.Security, SecurityRequirement.ToNode);
		obj.MaybeAddArray("servers", operation.Servers, Server.ToNode);
		obj.AddExtensions(operation.ExtensionData);

		return obj;
	}

	object? IRefTargetContainer.Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		int keysConsumed = 1;
		IRefTargetContainer? target = null;
		switch (keys[0])
		{
			case "externalDocs":
				target = ExternalDocs;
				break;
			case "parameters":
				if (keys.Length == 1) return null;
				keysConsumed++;
				target = Parameters?.GetFromArray(keys[1]);
				break;
			case "requestBody":
				target = RequestBody;
				break;
			case "responses":
				target = Responses;
				break;
			case "callbacks":
				if (keys.Length == 1) return null;
				keysConsumed++;
				target = Callbacks?.GetFromMap(keys[1]);
				break;
			case "servers":
				if (keys.Length == 1) return null;
				keysConsumed++;
				target = Servers?.GetFromArray(keys[1]);
				break;
		}

		return target != null
			? target.Resolve(keys[keysConsumed..])
			: ExtensionData?.Resolve(keys);
	}

	internal IEnumerable<JsonSchema> FindSchemas()
	{
		return GeneralHelpers.Collect(
			Parameters?.SelectMany(x => x.FindSchemas()),
			RequestBody?.FindSchemas(),
			Responses?.FindSchemas(),
			Callbacks?.Values.SelectMany(x => x.FindSchemas())
		);
	}

	internal IEnumerable<IComponentRef> FindRefs()
	{
		return GeneralHelpers.Collect(
			Parameters?.SelectMany(x => x.FindRefs()),
			RequestBody?.FindRefs(),
			Responses?.FindRefs(),
			Callbacks?.Values.SelectMany(x => x.FindRefs())
		);
	}
}

internal class OperationJsonConverter : JsonConverter<Operation>
{
	public override Operation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return Operation.FromNode(obj);
	}

	public override void Write(Utf8JsonWriter writer, Operation value, JsonSerializerOptions options)
	{
		var json = Operation.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}

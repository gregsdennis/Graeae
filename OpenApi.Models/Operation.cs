﻿using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OpenApi.Models;

[JsonConverter(typeof(OperationJsonConverter))]
public class Operation : IRefResolvable
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

	public IEnumerable<string>? Tags { get; set; }
	public string? Summary { get; set; }
	public string? Description { get; set; }
	public ExternalDocumentation? ExternalDocs { get; set; }
	public string? OperationId { get; set; }
	public IEnumerable<Parameter>? Parameters { get; set; }
	public RequestBody? RequestBody { get; set; }
	public ResponseCollection? Responses { get; set; }
	public Dictionary<string, Callback>? Callbacks { get; set; }
	public bool? Deprecated { get; set; }
	public IEnumerable<SecurityRequirement>? Security { get; set; }
	public IEnumerable<Server>? Servers { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public static Operation FromNode(JsonNode? node, JsonSerializerOptions? options)
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
			Parameters = obj.MaybeArray("parameters", x => Parameter.FromNode(x, options)),
			RequestBody = obj.Maybe("requestBody", x => RequestBody.FromNode(x, options)),
			Responses = obj.Maybe("responses", x => ResponseCollection.FromNode(x, options)),
			Callbacks = obj.MaybeMap("callbacks", x => Callback.FromNode(x, options)),
			Deprecated = obj.MaybeBool("deprecated", "operation"),
			Security = obj.MaybeArray("security", SecurityRequirement.FromNode),
			Servers = obj.MaybeArray("servers", Server.FromNode),
			ExtensionData = ExtensionData.FromNode(obj)
		};

		obj.ValidateNoExtraKeys(KnownKeys, operation.ExtensionData?.Keys);

		return operation;
	}

	public static JsonNode? ToNode(Operation? operation, JsonSerializerOptions? options)
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

	public object? Resolve(Span<string> keys)
	{
		if (keys.Length == 0) return this;

		int keysConsumed = 1;
		IRefResolvable? target = null;
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
}

public class OperationJsonConverter : JsonConverter<Operation>
{
	public override Operation? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return Operation.FromNode(obj, options);
	}

	public override void Write(Utf8JsonWriter writer, Operation value, JsonSerializerOptions options)
	{
		var json = Operation.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}

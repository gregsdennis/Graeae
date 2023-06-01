using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Schema;

namespace OpenApi.Models;

[JsonConverter(typeof(PathItemJsonConverter))]
public class PathItem : IRefTargetContainer
{
	private static readonly string[] KnownKeys =
	{
		"summary",
		"description",
		"get",
		"put",
		"post",
		"delete",
		"options",
		"head",
		"patch",
		"trace",
		"servers",
		"parameters"
	};

	public string? Summary { get; set; }
	public string? Description { get; set; }
	public Operation? Get { get; set; }
	public Operation? Put { get; set; }
	public Operation? Post { get; set; }
	public Operation? Delete { get; set; }
	public Operation? Options { get; set; }
	public Operation? Head { get; set; }
	public Operation? Patch { get; set; }
	public Operation? Trace { get; set; }
	public IEnumerable<Server>? Servers { get; set; }
	public IEnumerable<Parameter>? Parameters { get; set; }
	public ExtensionData? ExtensionData { get; set; }

	public static PathItem FromNode(JsonNode? node, JsonSerializerOptions? options)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		if (obj.ContainsKey("$ref"))
		{
			var item = new PathItemRef(obj.ExpectUri("$ref", "reference"))
			{
				Description = obj.MaybeString("description", "reference"),
				Summary = obj.MaybeString("summary", "reference")
			};

			obj.ValidateReferenceKeys();

			return item;
		}
		else
		{
			var item = new PathItem
			{
				Summary = obj.MaybeString("summary", "pathItem"),
				Description = obj.MaybeString("description", "pathItem"),
				Get = obj.TryGetPropertyValue("get", out var get) ? Operation.FromNode(get, options) : null,
				Put = obj.TryGetPropertyValue("put", out var put) ? Operation.FromNode(put, options) : null,
				Post = obj.TryGetPropertyValue("post", out var post) ? Operation.FromNode(post, options) : null,
				Delete = obj.TryGetPropertyValue("delete", out var delete) ? Operation.FromNode(delete, options) : null,
				Options = obj.TryGetPropertyValue("options", out var option) ? Operation.FromNode(option, options) : null,
				Head = obj.TryGetPropertyValue("head", out var head) ? Operation.FromNode(head, options) : null,
				Patch = obj.TryGetPropertyValue("patch", out var patch) ? Operation.FromNode(patch, options) : null,
				Trace = obj.TryGetPropertyValue("trace", out var trace) ? Operation.FromNode(trace, options) : null,
				Servers = obj.MaybeArray("servers", Server.FromNode),
				Parameters = obj.MaybeArray("parameters", x => Parameter.FromNode(x, options)),
				ExtensionData = ExtensionData.FromNode(obj)
			};

			obj.ValidateNoExtraKeys(KnownKeys, item.ExtensionData?.Keys);

			return item;
		}
	}

	public static JsonNode? ToNode(PathItem? item, JsonSerializerOptions? options)
	{
		if (item == null) return null;

		var obj = new JsonObject();

		if (item is PathItemRef reference)
		{
			obj.Add("$ref", reference.Ref.ToString());
			obj.MaybeAdd("description", reference.Description);
			obj.MaybeAdd("summary", reference.Summary);
		}
		else
		{
			obj.MaybeAdd("summary", item.Summary);
			obj.MaybeAdd("description", item.Description);
			obj.MaybeAdd("get", Operation.ToNode(item.Get, options));
			obj.MaybeAdd("put", Operation.ToNode(item.Put, options));
			obj.MaybeAdd("post", Operation.ToNode(item.Post, options));
			obj.MaybeAdd("delete", Operation.ToNode(item.Delete, options));
			obj.MaybeAdd("options", Operation.ToNode(item.Options, options));
			obj.MaybeAdd("head", Operation.ToNode(item.Head, options));
			obj.MaybeAdd("patch", Operation.ToNode(item.Patch, options));
			obj.MaybeAdd("trace", Operation.ToNode(item.Trace, options));
			obj.MaybeAddArray("servers", item.Servers, Server.ToNode);
			obj.MaybeAddArray("parameters", item.Parameters, x => Parameter.ToNode(x, options));
			obj.AddExtensions(item.ExtensionData);
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
			case "get":
				target = Get;
				break;
			case "put":
				target = Put;
				break;
			case "post":
				target = Post;
				break;
			case "delete":
				target = Delete;
				break;
			case "options":
				target = Options;
				break;
			case "head":
				target = Head;
				break;
			case "patch":
				target = Patch;
				break;
			case "trace":
				target = Trace;
				break;
			case "servers":
				if (keys.Length == 1) return null;
				keysConsumed++;
				target = Servers?.GetFromArray(keys[1]);
				break;
			case "parameters":
				if (keys.Length == 1) return null;
				keysConsumed++;
				target = Parameters?.GetFromArray(keys[1]);
				break;
		}

		return target != null
			? target.Resolve(keys[keysConsumed..])
			: ExtensionData?.Resolve(keys);
	}

	public IEnumerable<JsonSchema> FindSchemas()
	{
		return GeneralHelpers.Collect(
			Get?.FindSchemas(),
			Put?.FindSchemas(),
			Post?.FindSchemas(),
			Delete?.FindSchemas(),
			Options?.FindSchemas(),
			Head?.FindSchemas(),
			Patch?.FindSchemas(),
			Trace?.FindSchemas(),
			Parameters?.SelectMany(x => x.FindSchemas())
		);
	}

	public IEnumerable<IComponentRef> FindRefs()
	{
		if (this is PathItemRef piRef)
			yield return piRef;

		var theRest = GeneralHelpers.Collect(
			Get?.FindRefs(),
			Put?.FindRefs(),
			Post?.FindRefs(),
			Delete?.FindRefs(),
			Options?.FindRefs(),
			Head?.FindRefs(),
			Patch?.FindRefs(),
			Trace?.FindRefs(),
			Parameters?.SelectMany(x => x.FindRefs())
		);

		foreach (var compRef in theRest)
		{
			yield return compRef;
		}

		
	}
}

public class PathItemRef : PathItem, IComponentRef
{
	public Uri Ref { get; }
	public new string? Summary { get; set; }
	public new string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public PathItemRef(Uri reference)
	{
		Ref = reference ?? throw new ArgumentNullException(nameof(reference));
	}

	public void Resolve(OpenApiDocument root)
	{
		// resolve the $ref and set all of the props
		// remember to use base.*

		IsResolved = true;
	}
}

public class PathItemJsonConverter : JsonConverter<PathItem>
{
	public override PathItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ??
		          throw new JsonException("Expected an object");

		return PathItem.FromNode(obj, options);
	}

	public override void Write(Utf8JsonWriter writer, PathItem value, JsonSerializerOptions options)
	{
		var json = PathItem.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}

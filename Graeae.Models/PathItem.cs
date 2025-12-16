using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Schema;

namespace Graeae.Models;

/// <summary>
/// Models an individual path.
/// </summary>
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

	/// <summary>
	/// Gets or sets the summary.
	/// </summary>
	public string? Summary { get; set; }
	/// <summary>
	/// Gets or sets the description.
	/// </summary>
	public string? Description { get; set; }
	/// <summary>
	/// Gets or sets the GET operation.
	/// </summary>
	public Operation? Get { get; set; }
	/// <summary>
	/// Gets or sets the PUT operation.
	/// </summary>
	public Operation? Put { get; set; }
	/// <summary>
	/// Gets or sets the POST operation.
	/// </summary>
	public Operation? Post { get; set; }
	/// <summary>
	/// Gets or sets the DELETE operation.
	/// </summary>
	public Operation? Delete { get; set; }
	/// <summary>
	/// Gets or sets the OPTIONS operation.
	/// </summary>
	public Operation? Options { get; set; }
	/// <summary>
	/// Gets or sets the HEAD operation.
	/// </summary>
	public Operation? Head { get; set; }
	/// <summary>
	/// Gets or sets the PATCH operation.
	/// </summary>
	public Operation? Patch { get; set; }
	/// <summary>
	/// Gets or sets the TRACE operation.
	/// </summary>
	public Operation? Trace { get; set; }
	/// <summary>
	/// Gets or sets the collection of servers.
	/// </summary>
	public IReadOnlyList<Server>? Servers { get; set; }
	/// <summary>
	/// Gets or sets the collection of parameters.
	/// </summary>
	public IReadOnlyList<Parameter>? Parameters { get; set; }
	/// <summary>
	/// Gets or set extension data.
	/// </summary>
	public ExtensionData? ExtensionData { get; set; }

	internal static PathItem FromNode(JsonElement node, BuildOptions buildOptions)
	{
		if (node.ValueKind is not JsonValueKind.Object)
			throw new JsonException("Expected an object");

		PathItem item;
		if (node.TryGetProperty("$ref", out _))
		{
			item = new PathItemRef(node.ExpectUri("$ref", "reference"))
			{
				Description = node.MaybeString("description", "reference"),
				Summary = node.MaybeString("summary", "reference")
			};

			// PathItem is different from the other $ref-able objects in that the $ref is
			// integrated and the $ref'd values can be overridden.
			item.Import(node, buildOptions);

            node.ValidateReferenceKeys();
		}
		else
		{
			item = new PathItem();
			item.Import(node, buildOptions);

            node.ValidateNoExtraKeys(KnownKeys, item.ExtensionData?.Keys);
		}
		return item;
	}

	private protected void Import(JsonElement obj, BuildOptions buildOptions)
	{
		Summary = obj.MaybeString("summary", "pathItem");
		Description = obj.MaybeString("description", "pathItem");
		Get = obj.TryGetProperty("get", out var get) ? Operation.FromNode(get, buildOptions) : null;
		Put = obj.TryGetProperty("put", out var put) ? Operation.FromNode(put, buildOptions) : null;
		Post = obj.TryGetProperty("post", out var post) ? Operation.FromNode(post, buildOptions) : null;
		Delete = obj.TryGetProperty("delete", out var delete) ? Operation.FromNode(delete, buildOptions) : null;
		Options = obj.TryGetProperty("options", out var option) ? Operation.FromNode(option, buildOptions) : null;
		Head = obj.TryGetProperty("head", out var head) ? Operation.FromNode(head, buildOptions) : null;
		Patch = obj.TryGetProperty("patch", out var patch) ? Operation.FromNode(patch, buildOptions) : null;
		Trace = obj.TryGetProperty("trace", out var trace) ? Operation.FromNode(trace, buildOptions) : null;
		Servers = obj.MaybeArray("servers", Server.FromNode);
		Parameters = obj.MaybeArray("parameters", node => Parameter.FromNode(node, buildOptions));
		ExtensionData = ExtensionData.FromNode(obj);
	}

	internal static JsonNode? ToNode(PathItem? item, JsonSerializerOptions? options)
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

	object? IRefTargetContainer.Resolve(ReadOnlySpan<string> keys)
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
			? target.Resolve(keys.Slice(keysConsumed))
			: ExtensionData?.Resolve(keys);
	}

	internal IEnumerable<JsonSchema> FindSchemas()
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

	internal IEnumerable<IComponentRef> FindRefs()
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

/// <summary>
/// Models a `$ref` to a path item.
/// </summary>
public class PathItemRef : PathItem, IComponentRef
{
	/// <summary>
	/// The URI for the reference.
	/// </summary>
	public Uri Ref { get; }

	/// <summary>
	/// Gets or sets the summary.
	/// </summary>
	public new string? Summary { get; set; }

	/// <summary>
	/// Gets or sets the description.
	/// </summary>
	public new string? Description { get; set; }

	/// <summary>
	/// Gets whether the reference has been resolved.
	/// </summary>
	public bool IsResolved { get; private set; }

	/// <summary>
	/// Creates a new <see cref="PathItemRef"/>
	/// </summary>
	/// <param name="reference">The reference URI</param>
	public PathItemRef(Uri reference)
	{
		Ref = reference ?? throw new ArgumentNullException(nameof(reference));
	}

	/// <summary>
	/// Creates a new <see cref="PathItemRef"/>
	/// </summary>
	/// <param name="reference">The reference URI</param>
	public PathItemRef(string reference)
	{
		Ref = new Uri(reference ?? throw new ArgumentNullException(nameof(reference)), UriKind.RelativeOrAbsolute);
	}

	void IComponentRef.Resolve(OpenApiDocument root, BuildOptions buildOptions)
	{
		bool import(JsonElement? node)
		{
			if (node?.ValueKind is not JsonValueKind.Object) return false;

			Import(node.Value, buildOptions);
			return true;
		}

		void copy(PathItem other)
		{
			// PathItem is different from the other $ref-able objects in that the $ref is
			// integrated and the $ref'd values can be overridden.
			base.Summary = other.Summary;
			base.Description = other.Description;
			Get ??= other.Get;
			Put ??= other.Put;
			Post ??= other.Post;
			Delete ??= other.Delete;
			Options ??= other.Options;
			Head ??= other.Head;
			Patch ??= other.Patch;
			Trace ??= other.Trace;
			Servers ??= other.Servers;
			Parameters ??= other.Parameters;
			ExtensionData = other.ExtensionData;
		}

		IsResolved = Models.Ref.Resolve<PathItem>(root, Ref, import, copy);
	}
}

internal class PathItemJsonConverter : JsonConverter<PathItem>
{
	public override PathItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var obj = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
        if (obj.ValueKind is not JsonValueKind.Object)
            throw new JsonException("Expected an object");

		return PathItem.FromNode(obj, BuildOptions.Default);
	}

	public override void Write(Utf8JsonWriter writer, PathItem value, JsonSerializerOptions options)
	{
		var json = PathItem.ToNode(value, options);

		JsonSerializer.Serialize(writer, json, options);
	}
}

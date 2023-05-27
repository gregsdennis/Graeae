using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenApi.Models;

public class Callback : Dictionary<CallbackExpression, PathItem>
{
	public ExtensionData? ExtensionData { get; set; }

	public static Callback FromNode(JsonNode? node, JsonSerializerOptions? options)
	{
		if (node is not JsonObject obj)
			throw new JsonException("Expected an object");

		if (obj.ContainsKey("$ref"))
		{
			var callback = new CallbackRef(obj.ExpectUri("$ref", "reference"))
			{
				Description = obj.MaybeString("description", "reference"),
				Summary = obj.MaybeString("summary", "reference")
			};

			obj.ValidateReferenceKeys();

			return callback;
		}
		else
		{
			var callback = new Callback
			{
				ExtensionData = ExtensionData.FromNode(obj)
			};

			foreach (var (key, value) in obj)
			{
				if (key.StartsWith("x-")) continue;
				if (!CallbackExpression.TryParse(key, out var expression))
					throw new JsonException($"`{key}` is not a valid callback expression");

				callback.Add(expression, PathItem.FromNode(value, options));
			}

			// Validating extra keys is done in the loop.

			return callback;
		}
	}
}

public class CallbackRef : Callback
{
	public Uri Ref { get; }
	public string? Summary { get; set; }
	public string? Description { get; set; }

	public bool IsResolved { get; private set; }

	public CallbackRef(Uri reference)
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
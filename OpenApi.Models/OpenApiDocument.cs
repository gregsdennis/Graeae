using Json.Pointer;
using Json.Schema;

namespace OpenApi.Models;

public class OpenApiDocument : IBaseDocument
{
	public string OpenApi { get; }
	public OpenApiInfo Info { get; }
	public Uri? JsonSchemaDialect { get; set; }
	public IEnumerable<Server>? Servers { get; set; }
	public PathCollection? Paths { get; set; }
	public Dictionary<string, PathItem>? Webhooks { get; set; }
	public ComponentCollection? Components { get; set; }
	public IEnumerable<SecurityRequirement>? Security { get; set; }
	public IEnumerable<Tag>? Tags { get; set; }
	public ExternalDocumentation? ExternalDocs { get; set; }

	Uri IBaseDocument.BaseUri { get; }

	public OpenApiDocument(string openApi, OpenApiInfo info)
	{
		OpenApi = openApi ?? throw new ArgumentNullException(nameof(openApi));
		Info = info ?? throw new ArgumentNullException(nameof(info));
	}

	JsonSchema? IBaseDocument.FindSubschema(JsonPointer pointer, EvaluationOptions options)
	{
		throw new NotImplementedException();
	}
}
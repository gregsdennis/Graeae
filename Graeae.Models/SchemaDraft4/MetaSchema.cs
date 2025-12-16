using Json.Schema;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Graeae.Models.SchemaDraft4;

/// <summary>
/// Provides additional functionality for JSON Schema draft 4 support.
/// </summary>
public static class MetaSchema
{
    /// <summary>
	/// Defines the OpenAPI / JSON Schema draft 4 `file` type.
	/// </summary>
	public const SchemaValueType FileDataType = (SchemaValueType)(1 << 10);

    /// <summary>
    /// Defines the JSON Schema draft 4 meta-schema URI.
    /// </summary>
    public static Uri Draft4Id { get; } = new("http://json-schema.org/draft-04/schema#");

    /// <summary>
    /// Defines the JSON Schema draft 4 meta-schema.
    /// </summary>
    public static JsonSchema Draft4 { get; internal set; }   

    /// <summary>
    /// Registers all components required to use the OpenAPI vocabulary.
    /// </summary>
    public static void Register(BuildOptions? buildOptions = null)
    {
        buildOptions ??= BuildOptions.Default;

        buildOptions.DialectRegistry.Register(Dialect.Draft4);

        Draft4 = LoadMetaSchema("draft4", buildOptions);
    }

    private static JsonSchema LoadMetaSchema(string resourceName, BuildOptions buildOptions)
    {
        var resources = typeof(MetaSchemas).Assembly.GetManifestResourceNames();
        var resourceStream = typeof(MetaSchemas).Assembly.GetManifestResourceStream(@$"Json.Schema.OpenApi.Meta_Schemas._3._1.{resourceName}.json");
        using var reader = new StreamReader(resourceStream!, System.Text.Encoding.UTF8);

        var text = reader.ReadToEnd();
        var json = JsonDocument.Parse(text).RootElement;

        return JsonSchema.Build(json, buildOptions);
    }
}
using System.Text.Json;
using Json.Schema;

namespace Graeae.Models.SchemaDraft4;

/// <summary>
/// Provides the OpenAPI `nullable` keyword.
/// </summary>
public class NullableKeyword : IKeywordHandler
{
    public static NullableKeyword Instance { get; set; } = new();

    /// <summary>
    /// The name of the keyword.
    /// </summary>
    public string Name => "nullable";

    private NullableKeyword()
    {
    }

    public object? ValidateKeywordValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new JsonSchemaException($"'{Name}' value must be a boolean, found {value.ValueKind}")
        };
    }

    public void BuildSubschemas(KeywordData keyword, BuildContext context)
    {
    }

    public KeywordEvaluation Evaluate(KeywordData keyword, EvaluationContext context)
    {
        var nullable = (bool)keyword.Value!;
        return new KeywordEvaluation
        {
            Keyword = Name,
            IsValid = context.Instance.ValueKind is not JsonValueKind.Null || nullable
        };
    }
}
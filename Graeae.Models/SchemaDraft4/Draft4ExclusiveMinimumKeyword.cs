using System.Text.Json;
using Json.Schema;

namespace Graeae.Models.SchemaDraft4;

/// <summary>
/// Overrides the JSON Schema <see cref="ExclusiveMinimumKeyword"/> to support draft 4 boolean values.
/// </summary>
public class ExclusiveMinimumKeyword : IKeywordHandler
{
    public static ExclusiveMinimumKeyword Instance { get; set; } = new();

    /// <summary>
    /// The name of the keyword.
    /// </summary>
    public string Name => "exclusiveMinimum";

    private ExclusiveMinimumKeyword()
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
        return KeywordEvaluation.Ignore;
    }
}
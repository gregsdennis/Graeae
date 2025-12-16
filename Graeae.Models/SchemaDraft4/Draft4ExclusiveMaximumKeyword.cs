using System.Text.Json;
using Json.Schema;

namespace Graeae.Models.SchemaDraft4;

/// <summary>
/// Overrides the JSON Schema <see cref="ExclusiveMaximumKeyword"/> to support draft 4 boolean values.
/// </summary>
public class ExclusiveMaximumKeyword : IKeywordHandler
{
    public static ExclusiveMaximumKeyword Instance { get; set; } = new();

    /// <summary>
    /// The name of the keyword.
    /// </summary>
    public string Name => "exclusiveMaximum";

    private ExclusiveMaximumKeyword()
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
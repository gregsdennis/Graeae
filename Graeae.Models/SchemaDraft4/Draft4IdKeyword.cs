namespace Graeae.Models.SchemaDraft4;

/// <summary>
/// Represents the JSON Schema draft 4 `id` keyword.
/// </summary>
public class IdKeyword : Json.Schema.Keywords.Draft06.IdKeyword
{
    public new static IdKeyword Instance { get; set; } = new();
	
    /// <summary>
    /// The name of the keyword.
    /// </summary>
    public new string Name => "id";

    private IdKeyword()
    {
    }
}
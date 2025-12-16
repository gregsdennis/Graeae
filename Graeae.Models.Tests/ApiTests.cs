using Graeae.Models.SchemaDraft4;
using Json.Schema;

namespace Graeae.Models.Tests;

public class ApiTests
{
    [Test]
    public void InitializingAgainDoesNotThrow()
    {
        var options = new BuildOptions
        {
            Dialect = SchemaDraft4.Dialect.Draft4,
            SchemaRegistry = new()
        };
        MetaSchema.Register(options);
    }
}
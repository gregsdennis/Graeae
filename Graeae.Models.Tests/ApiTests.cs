using Graeae.Models.SchemaDraft4;

namespace Graeae.Models.Tests;

public class ApiTests
{
    [Test]
    public void InitializingAgainDoesNotThrow()
    {
        Draft4Support.Enable();
    }
}
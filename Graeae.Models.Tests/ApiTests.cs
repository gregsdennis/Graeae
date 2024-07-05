using Graeae.Models.SchemaDraft4;

namespace Graeae.Models.Tests;

public class ApiTests
{
    [Test]
    public void MultipleEnableDoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            Draft4Support.Enable();
            Draft4Support.Enable();
        });
    }
}
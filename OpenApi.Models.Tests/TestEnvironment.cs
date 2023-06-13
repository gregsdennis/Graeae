using OpenApi.Models.SchemaDraft4;

namespace OpenApi.Models.Tests;

[SetUpFixture]
public class TestEnvironment
{
	[OneTimeSetUp]
	public void Setup()
	{
#if !DEBUG
		EvaluationOptions.Default.Log = new TestLog();
#endif
		Draft4Support.Enable();
	}
}
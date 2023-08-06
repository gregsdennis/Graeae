using OpenApi.Models.SchemaDraft4;

namespace OpenApi.Models.Tests;

[SetUpFixture]
public class TestEnvironment
{
	[OneTimeSetUp]
	public void Setup()
	{
		Draft4Support.Enable();
	}
}
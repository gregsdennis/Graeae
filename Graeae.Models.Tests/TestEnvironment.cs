using Graeae.Models.SchemaDraft4;

namespace Graeae.Models.Tests;

[SetUpFixture]
public class TestEnvironment
{
	[OneTimeSetUp]
	public void Setup()
	{
		Draft4Support.Enable();
	}
}
namespace Graeae.Models.Tests;

public class RuntimeExpressionTests
{
	[TestCase("$method")]
	[TestCase("$url")]
	[TestCase("$statusCode")]
	[TestCase("$request.header.accept")]
	[TestCase("$response.body#/status")]
	[TestCase("$request.header.foo")]
	[TestCase("$request.query.bar")]
	[TestCase("$request.path.bar")]
	public void ParseTests_Success(string source)
	{
		var expression = RuntimeExpression.Parse(source);

		var backToString = expression.ToString();

		Console.WriteLine(backToString);
#pragma warning disable NUnit2005
		Assert.AreEqual(source, backToString);
#pragma warning restore NUnit2005
	}

	[TestCase("$unknown")]
	[TestCase("$abc")]
	[TestCase("$$")]
	[TestCase("$request.unknown.accept")]
	[TestCase("$response.unknown.accept")]
	[TestCase("$response.body")]
	[TestCase("$response.header")]
	[TestCase("$response.query")]
	[TestCase("$response.path")]
	[TestCase("$request.body")]
	[TestCase("$request.header")]
	[TestCase("$request.query")]
	[TestCase("$request.path")]
	[TestCase("$response.body$.not.a.pointer")]
	[TestCase("$response.body#$.not.a.pointer")]
	public void ParseTests_Failure(string source)
	{
		try
		{
			RuntimeExpression.Parse(source);
			Assert.Fail("Exception expected");
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
	}
}
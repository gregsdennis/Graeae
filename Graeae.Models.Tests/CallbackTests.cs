namespace Graeae.Models.Tests;

public class CallbackTests
{
	[Test]
	public void CallbackExpressionParse()
	{
		// example from https://spec.openapis.org/oas/v3.1.0#callback-object-examples
		var expr = (CallbackKeyExpression) "http://notificationServer.com?transactionId={$request.body#/id}&email={$request.body#/email}";

		Assert.AreEqual(2, expr.Parameters.Length);
		Assert.AreEqual("$request.body#/id", expr.Parameters[0]);
		Assert.AreEqual("$request.body#/email", expr.Parameters[1]);
	}
}
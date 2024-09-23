namespace Graeae.Models.Tests;

public class CallbackTests
{
	[Test]
	public void CallbackExpressionParse()
	{
		// example from https://spec.openapis.org/oas/v3.1.0#callback-object-examples
		var expr = (CallbackKeyExpression) "http://notificationServer.com?transactionId={$request.body#/id}&email={$request.body#/email}";

		Assert.That(expr.Parameters.Length, Is.EqualTo(2));
		Assert.That(expr.Parameters[0], Is.EqualTo("$request.body#/id"));
		Assert.That(expr.Parameters[1], Is.EqualTo("$request.body#/email"));
	}
}
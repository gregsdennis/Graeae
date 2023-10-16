namespace Graeae.AspNet.Tests.Host.RequestHandlers;

[RequestHandler("/hello")]
public static class HelloHandler
{
	public static Task<string> Get(HttpContext context)
	{
		return Task.FromResult("hello world");
	}
}
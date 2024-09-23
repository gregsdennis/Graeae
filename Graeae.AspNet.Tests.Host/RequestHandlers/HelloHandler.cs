using Microsoft.AspNetCore.Mvc;

namespace Graeae.AspNet.Tests.Host.RequestHandlers;

[RequestHandler("/hello")]
public static class HelloHandler
{
	public static IResult Get(HttpContext context, [FromQuery] string? name)
	{
		return TypedResults.Ok($"Hello, {name ?? "World"}");
	}
}
using Microsoft.AspNetCore.Mvc;

namespace Graeae.AspNet.Tests.Host.RequestHandlers;

[RequestHandler("/hello/{name}")]
public static class HelloNameHandler
{
	public static IResult Get(HttpContext context, [FromRoute] string? name)
	{
		return TypedResults.Ok($"Hello, {name ?? "World"}");
	}
}
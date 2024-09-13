using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Graeae.AspNet.Tests.Host.RequestHandlers;

[RequestHandler("/hello")]
public static class HelloHandler
{
	public static Ok<string> Get(HttpContext context, [FromQuery] string? name)
	{
		return TypedResults.Ok($"Hello, {name ?? "World"}");
	}

	public static Ok<string> Post(HttpContext context, [FromBody] string? name)
	{
		return TypedResults.Ok($"Hello, {name ?? "World"}");
	}
}

[RequestHandler("/hello/{name}")]
public static class HelloNameHandler
{
	public static Ok<string> Get(HttpContext context, [FromRoute] string? name)
	{
		return TypedResults.Ok($"Hello, {name ?? "World"}");
	}
}
using Microsoft.AspNetCore.Mvc;
using Graeae.AspNet.Tests.Host.RequestHandlers.Models;

namespace Graeae.AspNet.Tests.Host.RequestHandlers;

[RequestHandler("/goodbye")]
public static class GoodbyeHandler
{
	public static IResult Get(HttpContext context, [FromBody] Person? person)
	{
		return TypedResults.Ok($"Hello, {person?.Name ?? "World"}");
	}
}

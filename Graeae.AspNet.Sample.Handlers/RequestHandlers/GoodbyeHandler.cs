using Microsoft.AspNetCore.Mvc;

namespace Graeae.AspNet.Sample.Handlers.RequestHandlers;

[RequestHandler("/goodbye")]
public static class GoodbyeHandler
{
	public static IResult Post(HttpContext context, [FromBody] Person? person)
	{
		return TypedResults.Ok($"Hello, {person?.Name ?? "World"}");
	}
}

public class Person
{
	public string Name { get; set; }
	public int Age { get; set; }
}
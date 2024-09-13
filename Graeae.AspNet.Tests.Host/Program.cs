using Graeae.AspNet;

var builder = WebApplication.CreateBuilder();

var app = builder.Build();

await app.MapOpenApi("openapi.yaml", new OpenApiOptions
{
	IgnoreUnhandledPaths = true
});

app.Run();
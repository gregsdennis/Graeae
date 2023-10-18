using Graeae.AspNet;

var builder = WebApplication.CreateBuilder();

var app = builder.Build();

await app.MapOpenApi("openapi.yaml");

app.Run();
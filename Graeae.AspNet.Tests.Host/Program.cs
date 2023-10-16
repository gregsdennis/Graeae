using Graeae.AspNet;

var builder = WebApplication.CreateBuilder();

var app = builder.Build();

await app.MapOpenApi("service.yaml");

app.Run();
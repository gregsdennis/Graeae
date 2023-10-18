using System.Text;
using Graeae.AspNet.Analyzer;
using Microsoft.CodeAnalysis.Text;

using VerifyCS = Graeae.AspNet.Tests.Analyzer.Verifiers.CSharpSourceGeneratorVerifier<Graeae.AspNet.Analyzer.RouteVerifier>;

namespace Graeae.AspNet.Tests.Analyzer;

public class RouteVerifierTests
{
	[Test]
	public async Task Dev()
	{
		var openapiContent = await File.ReadAllTextAsync("openapi.yaml");

		var program = @"using Graeae.AspNet;

var builder = WebApplication.CreateBuilder();

var app = builder.Build();

await app.MapOpenApi(""openapi.yaml"");

app.Run();";
		var handler = @"namespace Graeae.AspNet.Tests.Host.RequestHandlers;

[RequestHandler(""/hello"")]
public static class HelloHandler
{
	public static Task<string> Get(HttpContext context)
	{
		return Task.FromResult(""hello world"");
	}
}";
		var generated = "expected generated code";

		await new VerifyCS.Test
		{
			TestState =
			{
				Sources = { program, handler },
				AdditionalFiles = { ("openapi.yaml", openapiContent) },
				GeneratedSources =
				{
					(typeof(RouteVerifier), "GeneratedFileName", SourceText.From(generated, Encoding.UTF8, SourceHashAlgorithm.Sha256)),
				},
			},
		}.RunAsync();
	}
}

using System.Collections.Immutable;
using Graeae.AspNet.Analyzer;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Graeae.AspNet.Tests.Analyzer.Verifiers.CSharpSourceGeneratorVerifier<Graeae.AspNet.Analyzer.MissingOperationsAnalyzer>;

namespace Graeae.AspNet.Tests.Analyzer;

public class MissingOperationsAnalyzerTests
{
	[Test]
	public async Task FoundHelloWarnAboutGoodbye()
	{
		var openapiContent = await File.ReadAllTextAsync("openapi.yaml");
		var attributeContent = @"using System;

namespace Graeae.AspNet;

[AttributeUsage(AttributeTargets.Class)]
public class RequestHandlerAttribute : Attribute
{
	public string Path { get; }

	public RequestHandlerAttribute(string path)
	{
		Path = path;
	}
}";

		var handler = @"using System.Threading.Tasks;
using Graeae.AspNet;
using Microsoft.AspNetCore.Http;

namespace Graeae.AspNet.Tests.Host.RequestHandlers;

[RequestHandler(""/hello"")]
public static class HelloHandler
{
	public static Task<string> Get(HttpContext context)
	{
		return Task.FromResult(""hello world"");
	}
}";

		await new VerifyCS.Test
		{
			TestState =
			{
				Sources = { handler, attributeContent },
				AdditionalFiles = { ("openapi.yaml", openapiContent) },
				ReferenceAssemblies = ReferenceAssemblies.Default.AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.AspNetCore.Http", "2.2.2"))),
				ExpectedDiagnostics =
				{
					new DiagnosticResult(Diagnostics.MissingRouteHandler("/goodbye").Descriptor)
				}
			},
		}.RunAsync();
	}
}

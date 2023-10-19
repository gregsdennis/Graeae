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
		var openapiContent = @"openapi: 3.1.0
info:
  title: Graeae Generation Test Host
  version: 1.0.0
paths:
  /hello:
    get:
      description: hello world
      responses:
        '200':
          description: okay
          content:
            application/json:
              schema:
                type: string
  /goodbye:
    get:
      description: goodbye world
      responses:
        '200':
          description: okay
          content:
            application/json:
              schema:
                type: string
";
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

	[Test]
	public async Task FoundGetHelloWarnPostHello()
	{
		var openapiContent = @"openapi: 3.1.0
info:
  title: Graeae Generation Test Host
  version: 1.0.0
paths:
  /hello:
    get:
      description: hello world
      responses:
        '200':
          description: okay
          content:
            application/json:
              schema:
                type: string
    post:
      description: goodbye world
      requestBody:
        content:
          application/json:
            schema:
              type: object
              properties:
                name:
                  type: string
              required:
                - name
      responses:
        '200':
          description: okay
          content:
            application/json:
              schema:
                type: string
";
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
					new DiagnosticResult(Diagnostics.MissingRouteOperationHandler("/hello", "Post").Descriptor)
				}
			},
		}.RunAsync();
	}
}

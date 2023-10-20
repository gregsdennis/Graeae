using Graeae.AspNet.Analyzer;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Graeae.AspNet.Tests.Analyzer.Verifiers.CSharpSourceGeneratorVerifier<Graeae.AspNet.Analyzer.MissingOperationsAnalyzer>;

namespace Graeae.AspNet.Tests.Analyzer;

public class MissingOperationsAnalyzerTests
{
	private const string AttributeContent = @"using System;

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

	[Test]
	public async Task FoundGetHelloWarnGoodbye()
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
				Sources = { handler, AttributeContent },
				AdditionalFiles = { ("openapi.yaml", openapiContent) },
				ReferenceAssemblies = PackageHelper.AspNetWeb,
				ExpectedDiagnostics =
				{
					new DiagnosticResult(Diagnostics.MissingRouteHandler("/goodbye").Descriptor)
				}
			}
		}.RunAsync();
	}

	[Test]
	public async Task MethodExistsWrongParams_Body()
	{
		var openapiContent = @"openapi: 3.1.0
info:
  title: Graeae Generation Test Host
  version: 1.0.0
paths:
  /hello:
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
		var handler = @"using System.Threading.Tasks;
using Graeae.AspNet;
using Microsoft.AspNetCore.Http;

namespace Graeae.AspNet.Tests.Host.RequestHandlers;

[RequestHandler(""/hello"")]
public static class HelloHandler
{
	public static Task<string> Post(HttpContext context)
	{
		return Task.FromResult(""hello world"");
	}
}";

		await new VerifyCS.Test
		{
			TestState =
			{
				Sources = { handler, AttributeContent },
				AdditionalFiles = { ("openapi.yaml", openapiContent) },
				ReferenceAssemblies = PackageHelper.AspNetWeb,
				ExpectedDiagnostics =
				{
					new DiagnosticResult(Diagnostics.MissingRouteOperationHandler("/hello", "Post").Descriptor)
				}
			}
		}.RunAsync();
	}

	[Test]
	public async Task MethodExistsWrongParams_Query()
	{
		var openapiContent = @"openapi: 3.1.0
info:
  title: Graeae Generation Test Host
  version: 1.0.0
paths:
  /hello:
    get:
      description: hello world
      parameters:
        - name: name
          in: query
          required: false
          schema:
            type: string
            format: int32
      responses:
        '200':
          description: okay
          content:
            application/json:
              schema:
                type: string
";
		var handler = @"using System.Threading.Tasks;
using Graeae.AspNet;
using Microsoft.AspNetCore.Http;

namespace Graeae.AspNet.Tests.Host.RequestHandlers;

[RequestHandler(""/hello"")]
public static class HelloHandler
{
	public static Task<string> Get(HttpContext context)
	{
		return Task.FromResult($""hello world"");
	}
}";

		await new VerifyCS.Test
		{
			TestState =
			{
				Sources = { handler, AttributeContent },
				AdditionalFiles = { ("openapi.yaml", openapiContent) },
				ReferenceAssemblies = PackageHelper.AspNetWeb,
				ExpectedDiagnostics =
				{
					new DiagnosticResult(Diagnostics.MissingRouteOperationHandler("/hello", "Get").Descriptor)
				}
			}
		}.RunAsync();
	}

	[Test]
	public async Task FoundGetHello_ImplicitQueryParam()
	{
		var openapiContent = @"openapi: 3.1.0
info:
  title: Graeae Generation Test Host
  version: 1.0.0
paths:
  /hello:
    get:
      description: hello world
      parameters:
        - name: name
          in: query
          required: false
          schema:
            type: string
            format: int32
      responses:
        '200':
          description: okay
          content:
            application/json:
              schema:
                type: string
";
		var handler = @"using System.Threading.Tasks;
using Graeae.AspNet;
using Microsoft.AspNetCore.Http;

namespace Graeae.AspNet.Tests.Host.RequestHandlers;

[RequestHandler(""/hello"")]
public static class HelloHandler
{
	public static Task<string> Get(HttpContext context, string name)
	{
		return Task.FromResult($""hello {name ?? ""world""}"");
	}
}";

		await new VerifyCS.Test
		{
			TestState =
			{
				Sources = { handler, AttributeContent },
				AdditionalFiles = { ("openapi.yaml", openapiContent) },
				ReferenceAssemblies = PackageHelper.AspNetWeb,
			}
		}.RunAsync();
	}

	[Test]
	public async Task FoundGetHello_ExplicitQueryParam()
	{
		var openapiContent = @"openapi: 3.1.0
info:
  title: Graeae Generation Test Host
  version: 1.0.0
paths:
  /hello:
    get:
      description: hello world
      parameters:
        - name: n
          in: query
          required: false
          schema:
            type: string
            format: int32
      responses:
        '200':
          description: okay
          content:
            application/json:
              schema:
                type: string
";
		var handler = @"using System.Threading.Tasks;
using Graeae.AspNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Graeae.AspNet.Tests.Host.RequestHandlers;

[RequestHandler(""/hello"")]
public static class HelloHandler
{
	public static Task<string> Get(HttpContext context, [FromQuery(Name = ""n"")] string name)
	{
		return Task.FromResult($""hello {name ?? ""world""}"");
	}
}";

		await new VerifyCS.Test
		{
			TestState =
			{
				Sources = { handler, AttributeContent },
				AdditionalFiles = { ("openapi.yaml", openapiContent) },
				ReferenceAssemblies = PackageHelper.AspNetWeb,
			}
		}.RunAsync();
	}

	[Test]
	public async Task FoundGetHello_ExplicitQueryParam_UnmatchedName()
	{
		var openapiContent = @"openapi: 3.1.0
info:
  title: Graeae Generation Test Host
  version: 1.0.0
paths:
  /hello:
    get:
      description: hello world
      parameters:
        - name: name
          in: query
          required: false
          schema:
            type: string
            format: int32
      responses:
        '200':
          description: okay
          content:
            application/json:
              schema:
                type: string
";
		var handler = @"using System.Threading.Tasks;
using Graeae.AspNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Graeae.AspNet.Tests.Host.RequestHandlers;

[RequestHandler(""/hello"")]
public static class HelloHandler
{
	public static Task<string> Get(HttpContext context, [FromQuery(Name = ""n"")] string name)
	{
		return Task.FromResult($""hello {name ?? ""world""}"");
	}
}";

		await new VerifyCS.Test
		{
			TestState =
			{
				Sources = { handler, AttributeContent },
				AdditionalFiles = { ("openapi.yaml", openapiContent) },
				ReferenceAssemblies = PackageHelper.AspNetWeb,
				ExpectedDiagnostics =
				{
					new DiagnosticResult(Diagnostics.MissingRouteOperationHandler("/hello", "Get").Descriptor)
				}
			}
		}.RunAsync();
	}

	[Test]
	public async Task FoundGetHello_ImplicitPathParam()
	{
		var openapiContent = @"openapi: 3.1.0
info:
  title: Graeae Generation Test Host
  version: 1.0.0
paths:
  /hello/{name}:
    get:
      description: hello world
      parameters:
        - name: name
          in: path
          required: false
          schema:
            type: string
            format: int32
      responses:
        '200':
          description: okay
          content:
            application/json:
              schema:
                type: string
";
		var handler = @"using System.Threading.Tasks;
using Graeae.AspNet;
using Microsoft.AspNetCore.Http;

namespace Graeae.AspNet.Tests.Host.RequestHandlers;

[RequestHandler(""/hello/{name}"")]
public static class HelloHandler
{
	public static Task<string> Get(HttpContext context, string name)
	{
		return Task.FromResult($""hello {name ?? ""world""}"");
	}
}";

		await new VerifyCS.Test
		{
			TestState =
			{
				Sources = { handler, AttributeContent },
				AdditionalFiles = { ("openapi.yaml", openapiContent) },
				ReferenceAssemblies = PackageHelper.AspNetWeb,
			}
		}.RunAsync();
	}

	[Test]
	public async Task FoundGetHello_ExplicitPathParam()
	{
		var openapiContent = @"openapi: 3.1.0
info:
  title: Graeae Generation Test Host
  version: 1.0.0
paths:
  /hello/{name}:
    get:
      description: hello world
      parameters:
        - name: name
          in: path
          required: false
          schema:
            type: string
            format: int32
      responses:
        '200':
          description: okay
          content:
            application/json:
              schema:
                type: string
";
		var handler = @"using System.Threading.Tasks;
using Graeae.AspNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Graeae.AspNet.Tests.Host.RequestHandlers;

[RequestHandler(""/hello/{name}"")]
public static class HelloHandler
{
	public static Task<string> Get(HttpContext context, [FromRoute(Name = ""name"")] string foo)
	{
		return Task.FromResult($""hello {foo ?? ""world""}"");
	}
}";

		await new VerifyCS.Test
		{
			TestState =
			{
				Sources = { handler, AttributeContent },
				AdditionalFiles = { ("openapi.yaml", openapiContent) },
				ReferenceAssemblies = PackageHelper.AspNetWeb,
			}
		}.RunAsync();
	}

	[Test]
	public async Task FoundGetHello_ExplicitPathParam_UnmatchedName()
	{
		var openapiContent = @"openapi: 3.1.0
info:
  title: Graeae Generation Test Host
  version: 1.0.0
paths:
  /hello/{name}:
    get:
      description: hello world
      parameters:
        - name: name
          in: path
          required: false
          schema:
            type: string
            format: int32
      responses:
        '200':
          description: okay
          content:
            application/json:
              schema:
                type: string
";
		var handler = @"using System.Threading.Tasks;
using Graeae.AspNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Graeae.AspNet.Tests.Host.RequestHandlers;

[RequestHandler(""/hello/{name}"")]
public static class HelloHandler
{
	public static Task<string> Get(HttpContext context, [FromRoute(Name = ""n"")] string name)
	{
		return Task.FromResult($""hello {name ?? ""world""}"");
	}
}";

		await new VerifyCS.Test
		{
			TestState =
			{
				Sources = { handler, AttributeContent },
				AdditionalFiles = { ("openapi.yaml", openapiContent) },
				ReferenceAssemblies = PackageHelper.AspNetWeb,
				ExpectedDiagnostics =
				{
					new DiagnosticResult(Diagnostics.MissingRouteOperationHandler("/hello/{name}", "Get").Descriptor)
				}
			}
		}.RunAsync();
	}
}
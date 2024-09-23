using System.Net;
using System.Text.Json.Nodes;
using Graeae.Models;
using Json.More;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Encoding = System.Text.Encoding;

namespace Graeae.AspNet.Tests;

public class RuntimeExpressionResolutionTests
{
	private HttpContext _context;
	private PathTemplate _pathTemplate;

	[OneTimeSetUp]
	public void Setup()
	{
		var requestContent = new JsonObject
		{
			["id"] = 1,
			["name"] = "item 1",
		}.AsJsonString();
		var requestBody = new MemoryStream();
		var requestWriter = new StreamWriter(requestBody, Encoding.UTF8);
		requestWriter.Write(requestContent);
		requestWriter.Flush();

		var responseContent = new JsonObject
		{
			["id"] = 1,
			["name"] = "item 1",
			["price"] = 3.14
		}.AsJsonString();
		var responseBody = new MemoryStream();
		var responseWriter = new StreamWriter(responseBody, Encoding.UTF8);
		responseWriter.Write(responseContent);
		responseWriter.Flush();

		_context = new DefaultHttpContext
		{
			Request =
			{
				Method = "PUT",
				IsHttps = true,
				Protocol = "https",
				ContentType = "application/json", // .net copies to headers
				Host = new HostString("graeae.net"),
				Path = "/items/1",
				Body = requestBody,
				Query = new QueryCollection(new Dictionary<string, StringValues> { ["param"] = "value" }),
				Headers =
				{
					["x-Authorization"] = "Bearer random_key"
				}
			},
			Response =
			{
				Body = responseBody,
				StatusCode = (int)HttpStatusCode.OK,
				ContentType = "application/json"
			}
		};

		_pathTemplate = PathTemplate.Parse("/items/{item}");
	}

	[TestCase("$url", "https://graeae.net/items/1?param=value")]
	[TestCase("$method", "PUT")]
	[TestCase("$statusCode", "200")]
	[TestCase("$request.header.Content-Type", "application/json")]
	[TestCase("$request.header.x-Authorization", "Bearer random_key")]
	[TestCase("$request.path.item", "1")]
	[TestCase("$request.query.param", "value")]
	[TestCase("$request.body#/name", "item 1")]
	[TestCase("$response.body#/price", "3.14")]
	[TestCase("$response.header.Content-Type", "application/json")]
	public void Test1(string runtimeExpression, string expected)
	{
		var expr = RuntimeExpression.Parse(runtimeExpression);
		var actual = expr.Resolve(_context, _pathTemplate);

		Assert.That(actual, Is.EqualTo(expected));
	}
}
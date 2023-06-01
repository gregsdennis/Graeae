using Json.Pointer;

namespace OpenApi.Models.Tests;

public class RefResolutionTests
{
	[Test]
	public void ResolvePathItem()
	{
		var file = "api-with-examples.yaml";
		var fullFileName = GetFile(file);

		var document = YamlSerializer.Deserialize<OpenApiDocument>(File.ReadAllText(fullFileName));

		var pathItem = document!.Find<PathItem>(JsonPointer.Parse("/paths/~1v2"));

		Assert.That(pathItem!.Get!.OperationId, Is.EqualTo("getVersionDetailsv2"));
	}
	[Test]
	public void ResolveExample()
	{
		var file = "api-with-examples.yaml";
		var fullFileName = GetFile(file);

		var document = YamlSerializer.Deserialize<OpenApiDocument>(File.ReadAllText(fullFileName));

		var example = document!.Find<Example>(JsonPointer.Parse("/paths/~1v2/get/responses/203/content/application~1json/examples/foo"));

		Assert.That(example!.Value!["version"]!["updated"]!.GetValue<string>(), Is.EqualTo("2011-01-21T11:33:21Z"));
	}
}
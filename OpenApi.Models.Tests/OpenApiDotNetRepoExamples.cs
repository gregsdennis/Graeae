using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml;
using Json.More;
using Yaml2JsonNode;
using YamlDotNet.RepresentationModel;

namespace OpenApi.Models.Tests;

public class OpenApiDotNetRepoExamples
{
	private static string GetFile(string name)
	{
		return Path.Combine(TestContext.CurrentContext.WorkDirectory, "Files", name)
			.AdjustForPlatform();
	}

	[TestCase("api-with-examples.yaml")]
	[TestCase("callback-example.yaml")]
	[TestCase("link-example.yaml")]
	[TestCase("non-oauth-scopes.yaml")]
	[TestCase("petstore.yaml")]
	[TestCase("petstore-expanded.yaml")]
	[TestCase("uspto.yaml")]
	[TestCase("webhook-example.yaml")]
	public void RoundTripYaml(string fileName)
	{
		var fullFileName = GetFile(fileName);

		var yaml = File.ReadAllText(fullFileName);

		try
		{
			var document = YamlSerializer.Deserialize<OpenApiDocument>(yaml);

			var returnToYaml = YamlSerializer.Serialize(document);

			Console.WriteLine(returnToYaml);
		}
		catch (Exception e)
		{
			Console.WriteLine($"{JsonSerializer.Serialize(e.Data)}");
			throw;
		}
	}

	[TestCase("api-with-examples.yaml")]
	[TestCase("callback-example.yaml")]
	[TestCase("link-example.yaml")]
	[TestCase("non-oauth-scopes.yaml")]
	[TestCase("petstore.yaml")]
	[TestCase("petstore-expanded.yaml")]
	[TestCase("uspto.yaml")]
	[TestCase("webhook-example.yaml")]
	public void RoundTripJson(string fileName)
	{
		var fullFileName = GetFile(fileName);

		var yamlText = File.ReadAllText(fullFileName);

		try
		{
			using var reader = new StringReader(yamlText);
			var yamlStream = new YamlStream();
			yamlStream.Load(reader);
			var yaml = yamlStream.Documents.First();
			var json = yaml.ToJsonNode();
			var document = json.Deserialize<OpenApiDocument>();

			var returnToJson = JsonSerializer.SerializeToNode(document, new JsonSerializerOptions
			{
				Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
			})!;

			Console.WriteLine(returnToJson);

			var returnToYaml = returnToJson.ToYamlNode();

			Console.WriteLine(YamlSerializer.Serialize(returnToYaml));
		}
		catch (Exception e)
		{
			Console.WriteLine($"{JsonSerializer.Serialize(e.Data)}");
			throw;
		}
	}
}
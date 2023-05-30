using System.Text;
using YamlDotNet.RepresentationModel;

namespace OpenApi.Models.Tests;

public class DevTest
{
	[Test]
	public void Test()
	{
//		var yamlText = @"rootProp1:
//  simpleValue: ""string""
//rootProp2:
//  simpleValue: ""different string""
//";
//		using var reader = new StringReader(yamlText);
//		var yamlStream = new YamlStream();
//		yamlStream.Load(reader);
//		//var yaml = yamlStream.Documents.First();

		var yaml = new YamlDocument(
			new YamlMappingNode(
				new KeyValuePair<YamlNode, YamlNode>(
					"rootProp1",
					new YamlMappingNode(new KeyValuePair<YamlNode, YamlNode>("simpleValue", "string"))
				),
				new KeyValuePair<YamlNode, YamlNode>(
					"rootProp2",
					new YamlMappingNode(new KeyValuePair<YamlNode, YamlNode>("simpleValue", "string"))
				)
			)
		);

		var newYamlStream = new YamlStream(yaml);
		var buffer = new StringBuilder();
		using var writer = new StringWriter(buffer);
		newYamlStream.Save(writer);

		var newYamlText = writer.ToString();

		Console.WriteLine(newYamlText);
	}
}
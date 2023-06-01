using System.Text;
using System.Text.Json;
using Yaml2JsonNode;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace OpenApi.Models;

public static class YamlSerializer
{
	public static string Serialize<T>(T obj, JsonSerializerOptions? options = null)
	{
		var json = JsonSerializer.SerializeToNode(obj, options);
		var yaml = json!.ToYamlNode();

		return Serialize(yaml);
	}

	public static string Serialize(YamlDocument yaml)
	{
		var serializer = new SerializerBuilder().Build();

		return serializer.Serialize(yaml.RootNode);
	}

	public static string Serialize(YamlNode yaml)
	{
		return Serialize(new YamlDocument(yaml));
	}

	public static T? Deserialize<T>(string yamlText, JsonSerializerOptions? options = null)
	{
		var yaml = Parse(yamlText);
		var json = yaml.ToJsonNode();

		return json.Deserialize<T>(options);
	}

	public static YamlDocument Parse(string yamlText)
	{
		using var reader = new StringReader(yamlText);
		var yamlStream = new YamlStream();
		yamlStream.Load(reader);
		var yaml = yamlStream.Documents.First();
		return yaml;
	}
}
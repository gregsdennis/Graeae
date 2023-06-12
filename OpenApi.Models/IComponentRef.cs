namespace OpenApi.Models;

public interface IComponentRef
{
	Uri Ref { get; }
	string? Summary { get; }
	string? Description { get; }

	Task Resolve(OpenApiDocument root);
}
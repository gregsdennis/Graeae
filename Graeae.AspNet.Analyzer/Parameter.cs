using Graeae.Models;

namespace Graeae.AspNet.Analyzer;

internal record Parameter
{
	public static readonly Parameter Body = new(string.Empty, ParameterLocation.Unspecified);

	public string Name { get; }
	public ParameterLocation In { get; }

	public Parameter(string name, ParameterLocation @in)
	{
		Name = @in == ParameterLocation.Header ? name.ToLowerInvariant() : name;
		In = @in;
	}
}
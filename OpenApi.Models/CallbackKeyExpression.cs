using System.Text.RegularExpressions;

namespace OpenApi.Models;

public class CallbackKeyExpression : IEquatable<string>
{
	private static readonly Regex TemplateVarsIdentifier = new(@"^([^{]*)(\{(?<runtimeExpr>[^}]+)\}([^{])*)*$");
	
	private readonly string _source;
	private IEnumerable<RuntimeExpression> _parameters;

	private CallbackKeyExpression(string source, IEnumerable<RuntimeExpression> parameters)
	{
		_source = source;
		_parameters = parameters;
	}

	public static CallbackKeyExpression Parse(string source)
	{
		var matches = TemplateVarsIdentifier.Matches(source);
		var parameters = matches.SelectMany(x => x.Groups["runtimeExpr"].Captures.Select(c => c.Value))
			.Select(RuntimeExpression.Parse);

		return new CallbackKeyExpression(source, parameters);
	}

	// likely needs an http request or something.
	public Uri Resolve()
	{
		throw new NotImplementedException();
	}

	public bool Equals(string? other)
	{
		return _source == other;
	}

	public override string ToString()
	{
		return _source;
	}
}
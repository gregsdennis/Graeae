using System.Text.RegularExpressions;

namespace OpenApi.Models;

/// <summary>
/// Models a callback key expression.
/// </summary>
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

	/// <summary>
	/// (not yet implemented) Resolves the callback expression.
	/// </summary>
	/// <returns>Throws not implemented.</returns>
	/// <exception cref="NotImplementedException">It's not implemented.</exception>
	// likely needs an http request or something.
	public Uri Resolve()
	{
		throw new NotImplementedException();
	}

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>
	/// <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.</returns>
	public bool Equals(string? other)
	{
		return _source == other;
	}

	/// <summary>Returns a string that represents the current object.</summary>
	/// <returns>A string that represents the current object.</returns>
	public override string ToString()
	{
		return _source;
	}
}
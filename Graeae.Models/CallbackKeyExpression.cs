using System.Linq;
using System.Text.RegularExpressions;

namespace Graeae.Models;

/// <summary>
/// Models a callback key expression.
/// </summary>
public class CallbackKeyExpression : IEquatable<string>
{
	private static readonly Regex TemplateVarsIdentifier = new(@"^([^{]*)(\{(?<runtimeExpr>[^}]+)\}([^{])*)*$");

	private readonly string _source;

	/// <summary>
	/// Gets the <see cref="RuntimeExpression"/> parameters that exist in the key expression.
	/// </summary>
	public RuntimeExpression[] Parameters { get; }

	private CallbackKeyExpression(string source, IEnumerable<RuntimeExpression> parameters)
	{
		_source = source;
		Parameters = parameters.ToArray();
	}

	internal static CallbackKeyExpression Parse(string source)
	{
		var matches = TemplateVarsIdentifier.Matches(source);
		var parameters = matches.Cast<Match>().SelectMany(x => x.Groups["runtimeExpr"].Captures.Cast<Capture>().Select(c => c.Value))
			.Select(RuntimeExpression.Parse);

		return new CallbackKeyExpression(source, parameters);
	}

	/// <summary>
	/// (not yet implemented) Resolves the callback expression.
	/// </summary>
	/// <returns>Throws not implemented.</returns>
	/// <exception cref="NotImplementedException">It's not implemented.</exception>
	/// <remarks>
	/// In order to implement this, an HttpRequest or HttpResponse is required, which
	/// means adding a reference to ASP.net.  As a result, it may make more sense for
	/// resolution functionality to exist in a secondary package.
	/// </remarks>
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

	/// <summary>
	/// Implicitly converts a string to a <see cref="CallbackKeyExpression"/> via parsing.
	/// </summary>
	/// <param name="source">A <see cref="CallbackKeyExpression"/></param>
	public static implicit operator CallbackKeyExpression(string source)
	{
		return Parse(source);
	}
}
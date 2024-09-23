using System.Diagnostics;

namespace Graeae.AspNet.Analyzer;

internal static class Debug
{
	[Conditional("DEBUG")]
	public static void Inject()
	{
		if (!Debugger.IsAttached) Debugger.Launch();
	}
}
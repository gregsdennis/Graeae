using System.Diagnostics;

namespace Graeae.AspNet.Analyzer;

internal static class Debug
{
	[Conditional("DEBUG")]
	public static void Break()
	{
		if (!Debugger.IsAttached) Debugger.Launch(); else Debugger.Break();
	}
}
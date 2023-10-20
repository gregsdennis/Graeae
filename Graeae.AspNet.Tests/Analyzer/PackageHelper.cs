using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Testing;

namespace Graeae.AspNet.Tests.Analyzer;

internal static class PackageHelper
{
	public static readonly ReferenceAssemblies AspNetWeb = ReferenceAssemblies.Net.Net60.AddPackages(
		("Microsoft.AspNetCore.App.Ref", "6.0.23")
	);

	public static ReferenceAssemblies AddPackages(this ReferenceAssemblies start, params (string name, string version)[] packages) =>
		start.AddPackages(packages
				.Select(x => new PackageIdentity(x.name, x.version))
				.ToImmutableArray()
			);
}
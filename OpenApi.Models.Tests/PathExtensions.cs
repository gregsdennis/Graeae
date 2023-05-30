namespace OpenApi.Models.Tests;

public static class PathExtensions
{
	public static string AdjustForPlatform(this string path)
	{
		return Environment.OSVersion.Platform == PlatformID.MacOSX ||
		       Environment.OSVersion.Platform == PlatformID.Unix
			? path.Replace("\\", "/")
			: path.Replace("/", "\\");
	}

	public static string GetFile(string name)
	{
		return Path.Combine(TestContext.CurrentContext.WorkDirectory, "Files", name)
			.AdjustForPlatform();
	}
}
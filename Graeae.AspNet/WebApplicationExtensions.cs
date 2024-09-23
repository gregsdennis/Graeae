using System.Linq.Expressions;
using System.Reflection;
using Graeae.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Yaml2JsonNode;

namespace Graeae.AspNet;

/// <summary>
/// Extends the app builder to scan an Open API document and automatically register methods.
/// </summary>
public static class WebApplicationExtensions
{
	/// <summary>
	/// Maps request handlers (see <see cref="RequestHandlerAttribute"/>) contained in the current assembly.
	/// </summary>
	/// <param name="app">The application builder</param>
	/// <param name="openApiFileName">The file name of the Open API document</param>
	/// <param name="options"></param>
	/// <returns>The application builder.</returns>
	public static async Task<IEndpointRouteBuilder> MapOpenApi(this IEndpointRouteBuilder app, string openApiFileName, OpenApiOptions? options = null)
	{
		options ??= OpenApiOptions.Default;

		var openApiText = await File.ReadAllTextAsync(openApiFileName);
		var openApiDocument = YamlSerializer.Deserialize<OpenApiDocument>(openApiText)!;

		await openApiDocument.Initialize();

		if (openApiDocument.Paths != null)
		{
			foreach (var (pathTemplate, pathItem) in openApiDocument.Paths)
			{
				MapPath(app, pathTemplate, pathItem, options);
			}
		}

		return app;
	}

	private static Type[]? _allEntryTypes;
	private static Type[] AllEntryTypes => _allEntryTypes ??= Assembly.GetEntryAssembly()!.GetTypes();

	private static void MapPath(IEndpointRouteBuilder app, PathTemplate pathTemplate, PathItem pathItem, OpenApiOptions options)
	{
		var path = pathTemplate.ToString();
		var type = AllEntryTypes.SingleOrDefault(x => x.GetCustomAttribute<RequestHandlerAttribute>()?.Path == path);
		if (type == null)
		{
			if (!options.IgnoreUnhandledPaths)
				throw new NotImplementedException($"A handler for '{path}' was not found.");

			return;
		}

		if (pathItem.Get is not null)
			MapOperation(app, nameof(pathItem.Get), path, type);
		if (pathItem.Post is not null)
			MapOperation(app, nameof(pathItem.Post), path, type);
		if (pathItem.Put is not null)
			MapOperation(app, nameof(pathItem.Put), path, type);
		if (pathItem.Delete is not null)
			MapOperation(app, nameof(pathItem.Delete), path, type);
		if (pathItem.Trace is not null)
			MapOperation(app, nameof(pathItem.Trace), path, type);
		if (pathItem.Head is not null)
			MapOperation(app, nameof(pathItem.Head), path, type);
		if (pathItem.Options is not null)
			MapOperation(app, nameof(pathItem.Options), path, type);
		if (pathItem.Patch is not null)
			MapOperation(app, nameof(pathItem.Patch), path, type);
	}

	private static void MapOperation(IEndpointRouteBuilder app, string action, string path, Type handlerType)
	{
		var handlerMethods = handlerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Where(x => x.Name == action);
		foreach (var handlerMethod in handlerMethods)
		{
			var handlerDelegate = CreateDelegate(handlerMethod);
			app.MapMethods(path, [action], handlerDelegate);
		}
	}

	private static Delegate CreateDelegate(this MethodInfo methodInfo)
	{
		Func<Type[], Type> getType;
		var types = methodInfo.GetParameters().Select(p => p.ParameterType);

		if (methodInfo.ReturnType == typeof(void))
			getType = Expression.GetActionType;
		else
		{
			getType = Expression.GetFuncType;
			types = types.Append(methodInfo.ReturnType);
		}

		return Delegate.CreateDelegate(getType(types.ToArray()), methodInfo);
	}
}
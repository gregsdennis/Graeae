﻿using System.Linq.Expressions;
using System.Reflection;
using Graeae.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Yaml2JsonNode;

namespace Graeae.AspNet;

public static class WebApplicationExtensions
{
	public static async Task<IEndpointRouteBuilder> MapOpenApi(this IEndpointRouteBuilder app, string openApiFileName)
	{
		var openApiText = await File.ReadAllTextAsync(openApiFileName);
		var openApiDocument = YamlSerializer.Deserialize<OpenApiDocument>(openApiText)!;

		await openApiDocument.Initialize();

		if (openApiDocument.Paths != null)
		{
			foreach (var (pathTemplate, pathItem) in openApiDocument.Paths)
			{
				MapPath(app, pathTemplate, pathItem);
			}
		}

		return app;
	}

	private static void MapPath(IEndpointRouteBuilder app, PathTemplate pathTemplate, PathItem pathItem)
	{
		var path = pathTemplate.ToString();
		var type = Assembly.GetEntryAssembly()!.GetTypes()
			           .SingleOrDefault(x => x.GetCustomAttribute<RequestHandlerAttribute>()?.Path == path)
		           ?? throw new NotImplementedException($"A handler for '{path}' was not found.");

		if (pathItem.Get is not null)
			MapOperation(app, nameof(pathItem.Get), path, type);
		if (pathItem.Get is not null)
			MapOperation(app, nameof(pathItem.Post), path, type);
		if (pathItem.Get is not null)
			MapOperation(app, nameof(pathItem.Put), path, type);
		if (pathItem.Get is not null)
			MapOperation(app, nameof(pathItem.Delete), path, type);
		if (pathItem.Get is not null)
			MapOperation(app, nameof(pathItem.Trace), path, type);
		if (pathItem.Get is not null)
			MapOperation(app, nameof(pathItem.Head), path, type);
		if (pathItem.Get is not null)
			MapOperation(app, nameof(pathItem.Options), path, type);
		if (pathItem.Get is not null)
			MapOperation(app, nameof(pathItem.Patch), path, type);
	}

	private static void MapOperation(IEndpointRouteBuilder app, string action, string path, Type handlerType)
	{
		var handlerMethods = handlerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Where(x => x.Name == action);
		foreach (var handlerMethod in handlerMethods)
		{
			var handlerDelegate = CreateDelegate(handlerMethod);
			app.MapMethods(path, new[] { action }, handlerDelegate);
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

[AttributeUsage(AttributeTargets.Class)]
public class RequestHandlerAttribute : Attribute
{
	public string Path { get; }

	public RequestHandlerAttribute(string path)
	{
		if (!PathTemplate.TryParse(path, out _))
			throw new ArgumentException($"'{path}' is not a valid path specifier.");

		Path = path;
	}
}
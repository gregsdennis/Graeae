using System.Text.Json;
using Json.Schema;
using Json.Schema.Generation.Serialization;
using Json.Schema.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Graeae.AspNet;

/// <summary>
/// A model binder that performs JSON Schema validation during deserialization.
/// </summary>
/// <remarks>
/// This binder supports binding models from the request body when the binding source is <see
/// cref="BindingSource.Body"/>. It uses the <see cref="JsonSerializer"/> with options configured via <see
/// cref="JsonOptions"/> to deserialize the JSON data. If validation errors are detected during deserialization, they
/// are added to the <see cref="ModelStateDictionary"/>.
/// </remarks>
public class ValidatingJsonModelBinder : IModelBinder
{
    /// <summary>Attempts to bind a model.</summary>
    /// <param name="bindingContext">The <see cref="T:Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext" />.</param>
    /// <returns>
    /// <para>
    /// A <see cref="T:System.Threading.Tasks.Task" /> which will complete when the model binding process completes.
    /// </para>
    /// <para>
    /// If model binding was successful, the <see cref="P:Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext.Result" /> should have
    /// <see cref="P:Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingResult.IsModelSet" /> set to <c>true</c>.
    /// </para>
    /// <para>
    /// A model binder that completes successfully should set <see cref="P:Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext.Result" /> to
    /// a value returned from <see cref="M:Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingResult.Success(System.Object)" />.
    /// </para>
    /// </returns>
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        // For body binding, we need to read the request body
        if (bindingContext.BindingSource == BindingSource.Body)
        {
            bindingContext.HttpContext.Request.EnableBuffering();
            using var reader = new StreamReader(bindingContext.HttpContext.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            bindingContext.HttpContext.Request.Body.Position = 0;

            if (string.IsNullOrEmpty(body))
            {
                return;
            }

            try
            {
                var options = bindingContext.HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
                var model = JsonSerializer.Deserialize(body, bindingContext.ModelType, options);
                bindingContext.Result = ModelBindingResult.Success(model);
            }
            catch (JsonException jsonException)
            {
                if (jsonException.Data.Contains("validation") &&
                    jsonException.Data["validation"] is EvaluationResults validationResults)
                {
                    var errors = ExtractValidationErrors(validationResults);
                    if (errors.Any())
                    {
                        foreach (var error in errors)
                        {
                            bindingContext.ModelState.AddModelError(error.Path, error.Message);
                        }
                        bindingContext.Result = ModelBindingResult.Failed();
                        return;
                    }
                }

                bindingContext.ModelState.AddModelError(bindingContext.FieldName, jsonException, bindingContext.ModelMetadata);
                bindingContext.Result = ModelBindingResult.Failed();
            }
            return;
        }

        // For other binding sources, use the value provider
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            return;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

        try
        {
            var value = valueProviderResult.FirstValue;
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            var options = bindingContext.HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
            var model = JsonSerializer.Deserialize(value, bindingContext.ModelType, options);
            bindingContext.Result = ModelBindingResult.Success(model);
        }
        catch (JsonException jsonException)
        {
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, jsonException, bindingContext.ModelMetadata);
            bindingContext.Result = ModelBindingResult.Failed();
        }
    }

    static List<(string Path, string Message)> ExtractValidationErrors(EvaluationResults validationResults)
    {
        var errors = new List<(string Path, string Message)>();
        ExtractValidationErrorsRecursive(validationResults, errors);
        return errors;
    }

    static void ExtractValidationErrorsRecursive(EvaluationResults results, List<(string Path, string Message)> errors)
    {
        if (results.IsValid)
        {
            return;
        }

        if (results.Errors != null)
        {
            foreach (var error in results.Errors)
            {
                errors.Add((results.InstanceLocation.ToString(), error.Value));
            }
        }

        foreach (var detail in results.Details)
        {
            ExtractValidationErrorsRecursive(detail, errors);
        }
    }
}

/// <summary>
/// Provides a model binder that validates JSON input against a schema for types annotated with specific attributes.
/// </summary>
/// <remarks>
/// This provider creates a <see cref="ValidatingJsonModelBinder"/> for model types that are decorated
/// with  <see cref="GenerateJsonSchemaAttribute"/> or <see cref="JsonSchemaAttribute"/>. The binder is applied only 
/// when the binding source is either undefined or explicitly set to <see cref="BindingSource.Body"/>.
/// </remarks>
public class ValidatingJsonModelBinderProvider : IModelBinderProvider
{
    /// <summary>
    /// Creates a <see cref="T:Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder" /> based on <see cref="T:Microsoft.AspNetCore.Mvc.ModelBinding.ModelBinderProviderContext" />.
    /// </summary>
    /// <param name="context">The <see cref="T:Microsoft.AspNetCore.Mvc.ModelBinding.ModelBinderProviderContext" />.</param>
    /// <returns>An <see cref="T:Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder" />.</returns>
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // Only use this binder for types that have the GenerateJsonSchema attribute
        // and only if the binding source is undefined or explicitly the body
        if ((context.Metadata.BindingSource == null || context.Metadata.BindingSource == BindingSource.Body) &&
            (context.Metadata.ModelType.GetCustomAttributes(typeof(GenerateJsonSchemaAttribute), true).Any() ||
             context.Metadata.ModelType.GetCustomAttributes(typeof(JsonSchemaAttribute), true).Any()))
        {
            return new ValidatingJsonModelBinder();
        }

        return null;
    }
}
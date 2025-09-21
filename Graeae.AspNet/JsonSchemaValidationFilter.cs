using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Graeae.AspNet;

/// <summary>
/// This filter translates JSON Schema model binding failures produced by <see cref="ValidatingJsonModelBinder"/>
/// into an RFC 9457 Problem Details response format.
/// </summary>
/// <remarks>
/// This filter ensures that JSON input adheres to the defined JSON Schema by checking the <see cref="FilterContext.ModelState"/>
/// for validation errors. If validation errors are detected, a <see cref="ProblemDetails"/>
/// response is returned with detailed error information. The filter operates during the action execution phase.
/// </remarks>
public class JsonSchemaValidationFilter : IActionFilter, IAlwaysRunResultFilter
{
    public string? ProblemTypeUri { get; set; }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        // this method is required for partial binding success
        var check = HandleJsonSchemaErrors(context);
        if (check is not null)
        {
            context.Result = check;
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // no-op
    }

    public void OnResultExecuting(ResultExecutingContext context)
    {
        // this method is required for total binding failure
        var check = HandleJsonSchemaErrors(context);
        if (check is not null)
        {
            context.Result = check;
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
        // no-op
    }

    IActionResult? HandleJsonSchemaErrors(FilterContext context)
    {
        if (context.ModelState.IsValid)
        {
            return null;
        }

        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Any() == true)
            .SelectMany(x => x.Value!.Errors.Select(e => new
            {
                Path = x.Key,
                Message = e.ErrorMessage,
            }))
            .Where(e => string.IsNullOrEmpty(e.Path) || e.Path.StartsWith('/')) // empty JSON Pointer is empty string
            .GroupBy(x => x.Path)
            .ToDictionary(x => x.Key, x => x.Select(e => e.Message).ToList());

        if (errors.Count == 0)
        {
            // If we don't have JSON Pointer errors, JSON Schema didn't handle this.
            // Don't change anything.
            return null;
        }

        var problemDetails = new ProblemDetails
        {
            Type = ProblemTypeUri ?? "https://json-everything.net/rfc9457/validation-error",
            Title = "JSON Schema Validation Error",
            Status = 400,
            Detail = "One or more validation errors occurred. See the 'errors' property for more information",
            Extensions =
            {
                ["errors"] = errors,
            },
        };

        return new BadRequestObjectResult(problemDetails);

    }
}
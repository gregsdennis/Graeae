using System.Text.Json;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Graeae.AspNet;

public static partial class WebApplicationExtensions
{
    public static IMvcBuilder GenerateRequestSchemas(this IMvcBuilder builder)
    {
        builder.AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            o.JsonSerializerOptions.Converters.Add(new GenerativeValidatingJsonConverter
                {
                    Options =
                    {
                        OutputFormat = OutputFormat.Hierarchical,
                        RequireFormatValidation = true,
                    },
                    GeneratorConfiguration =
                    {
                        PropertyNameResolver = PropertyNameResolvers.CamelCase
                    }
                }
            );
        });

        return builder;
    }

    public static MvcOptions AddJsonSchemaValidation(this MvcOptions options)
    {
        options.Filters.Add<JsonSchemaValidationFilter>();
        options.ModelBinderProviders.Insert(0, new ValidatingJsonModelBinderProvider());

        return options;
    }
}
using System.Reflection;
using Microsoft.OpenApi.Models;

namespace SurveyBot.API.Extensions;

/// <summary>
/// Extension methods for configuring Swagger/OpenAPI documentation.
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// Adds Swagger generation services with JWT authentication and XML comments.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            // API Information
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SurveyBot API",
                Version = "v1.0",
                Description = "RESTful API for the Telegram Survey Bot MVP - Manage surveys, questions, and responses.",
                Contact = new OpenApiContact
                {
                    Name = "SurveyBot Team",
                    Email = "support@surveybot.com",
                    Url = new Uri("https://github.com/surveybot/surveybot")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // JWT Bearer Authentication
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // XML Comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Tag ordering
            options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] ?? "Default" });
            options.OrderActionsBy(api => api.RelativePath);

            // Enable annotations (requires Swashbuckle.AspNetCore.Annotations package)
            // options.EnableAnnotations();

            // Custom operation filters for better documentation
            options.OperationFilter<SwaggerDefaultValues>();
        });

        return services;
    }

    /// <summary>
    /// Configures Swagger UI middleware with custom settings.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app)
    {
        app.UseSwagger(options =>
        {
            options.SerializeAsV2 = false;
        });

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "SurveyBot API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "SurveyBot API Documentation";

            // Enable deep linking
            options.EnableDeepLinking();

            // Display request duration
            options.DisplayRequestDuration();

            // Enable filter
            options.EnableFilter();

            // Enable try it out by default
            options.EnableTryItOutByDefault();

            // Persist authorization
            options.EnablePersistAuthorization();

            // Custom CSS for better appearance (optional)
            options.InjectStylesheet("/swagger-ui/custom.css");
        });

        return app;
    }
}

/// <summary>
/// Swagger operation filter to add default values and descriptions.
/// </summary>
public class SwaggerDefaultValues : Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter
{
    /// <summary>
    /// Applies default values to Swagger operations.
    /// </summary>
    /// <param name="operation">The Swagger operation.</param>
    /// <param name="context">The operation filter context.</param>
    public void Apply(OpenApiOperation operation, Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
    {
        // Add response examples
        if (operation.Responses.ContainsKey("401"))
        {
            operation.Responses["401"].Description = "Unauthorized - Invalid or missing JWT token";
        }

        if (operation.Responses.ContainsKey("403"))
        {
            operation.Responses["403"].Description = "Forbidden - Insufficient permissions";
        }

        if (operation.Responses.ContainsKey("404"))
        {
            operation.Responses["404"].Description = "Not Found - Resource does not exist";
        }

        if (operation.Responses.ContainsKey("500"))
        {
            operation.Responses["500"].Description = "Internal Server Error - An unexpected error occurred";
        }

        // Set operation ID if not already set
        if (string.IsNullOrEmpty(operation.OperationId))
        {
            operation.OperationId = context.MethodInfo.Name;
        }
    }
}

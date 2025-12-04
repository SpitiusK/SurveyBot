using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SurveyBot.API.Extensions;
using SurveyBot.API.Filters;
using SurveyBot.Bot.Extensions;
using SurveyBot.Core.Configuration;
using SurveyBot.Core.Interfaces;
using SurveyBot.Infrastructure;
using SurveyBot.Infrastructure.Data;
using SurveyBot.Infrastructure.Services;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "SurveyBot.API")
    .CreateLogger();

try
{
    Log.Information("Starting SurveyBot API application");

    var builder = WebApplication.CreateBuilder(args);

    // Replace default logging with Serilog
    builder.Host.UseSerilog();

    // Add services to the container.

    // Add Controllers with Model Validation Logging Filter
    builder.Services.AddControllers(options =>
    {
        // Add filter that logs detailed validation errors before 400 Bad Request
        options.Filters.Add<ModelValidationLoggingFilter>();
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Disable automatic 400 response so our filter can run and log validation errors
        options.SuppressModelStateInvalidFilter = true;
    });

    // Register the validation logging filter
    builder.Services.AddScoped<ModelValidationLoggingFilter>();

    // Configure CORS for frontend access
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(
                      "http://localhost:3000",                  // Local frontend (React dev server)
                      "http://localhost:5173",                  // Local frontend (Vite)
                      "https://7e31d418b2ac.ngrok-free.app",    // ngrok frontend URL
                      "https://4fb96e092b87.ngrok-free.app"     // ngrok backend URL
                   )
                  .SetIsOriginAllowedToAllowWildcardSubdomains() // Allow ngrok subdomains
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });

        // Add permissive policy for ngrok specifically (allows any ngrok URL dynamically)
        options.AddPolicy("NgrokPolicy", policy =>
        {
            policy.SetIsOriginAllowed(origin =>
                   {
                       // Allow all ngrok URLs
                       return origin.Contains("ngrok-free.app") ||
                              origin.Contains("ngrok.app") ||
                              origin.Contains("ngrok.io") ||
                              origin == "http://localhost:3000" ||
                              origin == "http://localhost:5173";
                   })
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // Configure AutoMapper
    builder.Services.AddAutoMapper(typeof(Program).Assembly);

    // Configure Entity Framework Core with PostgreSQL
    // Skip DbContext registration in Testing environment (integration tests will register their own)
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddDbContext<SurveyBotDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            
            // Log the connection string for debugging (mask password)
            var maskedConnectionString = connectionString?.Replace("Password=surveybot_dev_password", "Password=***");
            Console.WriteLine($"[DEBUG] Using connection string: {maskedConnectionString}");
            Log.Information("Database connection string configured: {ConnectionString}", maskedConnectionString);
            
            options.UseNpgsql(connectionString);

            // Enable detailed logging in development
            if (builder.Environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });
    }

    // Configure JWT Settings
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
    var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

    if (jwtSettings == null || string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
    {
        throw new InvalidOperationException("JWT settings are not properly configured in appsettings.json");
    }

    // Configure JWT Authentication
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = false; // Set to true in production
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero // Remove default 5 minute clock skew
        };

        // Add logging for JWT authentication events
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Log.Warning("JWT Authentication failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                Log.Debug("JWT token validated for user ID: {UserId}", userId);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Log.Warning("JWT authentication challenge: {Error}", context.ErrorDescription);
                return Task.CompletedTask;
            }
        };
    });

    // Add Authorization
    builder.Services.AddAuthorization();

    // Register Infrastructure layer services (includes repositories and services)
    // MUST be registered BEFORE bot handlers because handlers depend on infrastructure services
    // Pass environment name to prevent dual database provider registration in Testing environment
    builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.EnvironmentName);

    // Register Media Storage Service (requires IWebHostEnvironment from ASP.NET Core)
    builder.Services.AddScoped<IMediaStorageService>(sp =>
    {
        var env = sp.GetRequiredService<IWebHostEnvironment>();
        var logger = sp.GetRequiredService<ILogger<FileSystemMediaStorageService>>();
        var validationService = sp.GetRequiredService<IMediaValidationService>();
        var configuration = sp.GetRequiredService<IConfiguration>();

        // Get webRootPath with multi-level fallback logic
        string webRootPath;

        // Priority 1: Check for explicit configuration in appsettings.json
        var configuredPath = configuration.GetValue<string>("MediaStorage:StoragePath");
        if (!string.IsNullOrEmpty(configuredPath))
        {
            // Use explicitly configured path (useful for Docker volumes)
            webRootPath = configuredPath;
            logger.LogInformation(
                "Using configured MediaStorage:StoragePath: {WebRootPath}",
                webRootPath);
        }
        // Priority 2: Use IWebHostEnvironment.WebRootPath if available
        else if (!string.IsNullOrEmpty(env.WebRootPath))
        {
            webRootPath = env.WebRootPath;
            logger.LogInformation(
                "Using WebRootPath from IWebHostEnvironment: {WebRootPath}",
                webRootPath);
        }
        // Priority 3: Fallback to ContentRootPath/wwwroot
        else
        {
            webRootPath = Path.Combine(env.ContentRootPath, "wwwroot");
            logger.LogWarning(
                "WebRootPath was null. Using fallback: {WebRootPath}",
                webRootPath);
        }

        // Ensure directory exists with proper error handling
        try
        {
            if (!Directory.Exists(webRootPath))
            {
                Directory.CreateDirectory(webRootPath);
                logger.LogInformation("Created media storage directory: {WebRootPath}", webRootPath);
            }

            // Test write permissions by creating uploads/media subdirectory
            var uploadsPath = Path.Combine(webRootPath, "uploads", "media");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
                logger.LogInformation("Created media uploads directory: {UploadsPath}", uploadsPath);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError(ex,
                "Access denied creating media storage directory: {WebRootPath}",
                webRootPath);
            throw new InvalidOperationException(
                $"Cannot create media storage directory (access denied): {webRootPath}", ex);
        }
        catch (IOException ex)
        {
            logger.LogError(ex,
                "I/O error creating media storage directory: {WebRootPath}",
                webRootPath);
            throw new InvalidOperationException(
                $"Cannot create media storage directory (I/O error): {webRootPath}", ex);
        }

        // Final validation
        if (!Directory.Exists(webRootPath))
        {
            logger.LogError("Media storage directory does not exist: {WebRootPath}", webRootPath);
            throw new InvalidOperationException(
                $"Media storage directory does not exist and could not be created: {webRootPath}");
        }

        logger.LogInformation(
            "Media storage initialized successfully at: {WebRootPath}",
            webRootPath);

        return new FileSystemMediaStorageService(webRootPath, logger, validationService);
    });

    // Register Telegram Bot Services (AFTER infrastructure because it has handlers that depend on infrastructure)
    builder.Services.AddTelegramBot(builder.Configuration);
    builder.Services.AddBotHandlers();

    // Register Background Task Queue for webhook processing
    builder.Services.AddBackgroundTaskQueue(queueCapacity: 100);

    // Add Health Checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<SurveyBotDbContext>(
            name: "database",
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
            tags: new[] { "db", "sql", "postgresql" });

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "SurveyBot API",
            Version = "v1",
            Description = "REST API for Telegram Survey Bot - Managing surveys, questions, and responses",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "SurveyBot API Team",
                Email = "support@surveybot.com"
            }
        });

        // Enable XML documentation comments
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }

        // Configure JWT Bearer Authentication for Swagger
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below. Example: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'"
        });

        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    // Apply database migrations automatically (skip in Testing environment)
    if (!app.Environment.IsEnvironment("Testing"))
    {
        try
        {
            Log.Information("Applying database migrations...");
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SurveyBotDbContext>();

                // Check if database can connect
                var canConnect = await dbContext.Database.CanConnectAsync();
                if (!canConnect)
            {
                Log.Warning("Cannot connect to database. Waiting for database to be ready...");
                // Retry logic for database connection (useful for Docker startup)
                int retries = 10;
                int delay = 3000; // 3 seconds

                for (int i = 0; i < retries; i++)
                {
                    await Task.Delay(delay);
                    canConnect = await dbContext.Database.CanConnectAsync();
                    if (canConnect)
                    {
                        Log.Information("Database connection established after {Retries} retries", i + 1);
                        break;
                    }
                    Log.Warning("Retry {RetryCount}/{MaxRetries} - Database not ready yet", i + 1, retries);
                }

                if (!canConnect)
                {
                    Log.Error("Failed to connect to database after {Retries} retries", retries);
                    throw new InvalidOperationException("Cannot connect to database");
                }
            }

            // Apply pending migrations
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                Log.Information("Found {Count} pending migrations. Applying...", pendingMigrations.Count());
                await dbContext.Database.MigrateAsync();
                Log.Information("Database migrations applied successfully");
            }
            else
            {
                Log.Information("Database is up to date. No pending migrations");
            }
        }
    }
    catch (Exception ex)
        {
            Log.Error(ex, "Error applying database migrations");
            throw; // Don't continue if migrations fail
        }
    }

    // Initialize Telegram Bot
    try
    {
        await app.Services.InitializeTelegramBotAsync();
        Log.Information("Telegram Bot initialized successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to initialize Telegram Bot. The application will continue without bot functionality.");
        // Don't throw - allow API to start even if bot initialization fails
    }

    // Verify DI Configuration in Development
    if (app.Environment.IsDevelopment())
    {
        var (success, errors) = SurveyBot.API.DIVerifier.VerifyServiceResolution(app.Services);
        SurveyBot.API.DIVerifier.PrintVerificationResults(success, errors);
    }

    // Configure the HTTP request pipeline.

    // Enable request body buffering so we can read it multiple times (for logging)
    app.Use(async (context, next) =>
    {
        context.Request.EnableBuffering();
        await next();
    });

    // Add Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress);
        };
    });

    // Add custom request logging middleware
    app.UseRequestLogging();

    // Add global exception handling middleware (should be early in pipeline)
    app.UseGlobalExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "SurveyBot API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "SurveyBot API Documentation";
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
            options.ShowExtensions();
        });
    }

    // Enable CORS (must be before UseAuthentication)
    // Using NgrokPolicy to auto-allow all ngrok URLs dynamically
    app.UseCors("NgrokPolicy");

    // Enable static file serving for media uploads
    var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    if (!Directory.Exists(webRootPath))
    {
        Directory.CreateDirectory(webRootPath);
    }

    var staticFileOptions = new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(webRootPath),
        RequestPath = "",
        ContentTypeProvider = new FileExtensionContentTypeProvider()
    };
    app.UseStaticFiles(staticFileOptions);

    app.UseHttpsRedirection();

    // Add Authentication & Authorization middleware
    app.UseAuthentication();
    app.UseAuthorization();

    // Map Controllers
    app.MapControllers();

    // Map Health Check Endpoints
    app.MapHealthChecks("/health/db");

    // Database health check endpoint with detailed info (minimal API for DB-specific check)
    app.MapGet("/health/db/details", async (SurveyBotDbContext dbContext) =>
    {
        try
        {
            // Try to connect to the database
            var canConnect = await dbContext.Database.CanConnectAsync();

            if (canConnect)
            {
                return Results.Ok(new
                {
                    status = "healthy",
                    database = "connected",
                    timestamp = DateTime.UtcNow
                });
            }

            return Results.Problem(
                detail: "Cannot connect to database",
                statusCode: StatusCodes.Status503ServiceUnavailable
            );
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: $"Database connection error: {ex.Message}",
                statusCode: StatusCodes.Status503ServiceUnavailable
            );
        }
    })
    .WithName("DatabaseHealthCheckDetails")
    .WithOpenApi()
    .WithTags("Health");

    Log.Information("SurveyBot API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program public for integration tests (WebApplicationFactory)
public partial class Program { }

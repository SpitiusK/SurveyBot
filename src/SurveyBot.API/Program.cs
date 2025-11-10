using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SurveyBot.API.Extensions;
using SurveyBot.Bot.Extensions;
using SurveyBot.Core.Configuration;
using SurveyBot.Core.Interfaces;
using SurveyBot.Infrastructure.Data;
using SurveyBot.Infrastructure.Repositories;
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

    // Add Controllers
    builder.Services.AddControllers();

    // Configure AutoMapper
    builder.Services.AddAutoMapper(typeof(Program).Assembly);

    // Configure Entity Framework Core with PostgreSQL
    builder.Services.AddDbContext<SurveyBotDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(connectionString);

        // Enable detailed logging in development
        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
    });

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

    // Register Repository Implementations (Scoped)
    builder.Services.AddScoped<ISurveyRepository, SurveyRepository>();
    builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
    builder.Services.AddScoped<IResponseRepository, ResponseRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IAnswerRepository, AnswerRepository>();

    // Register Service Implementations (Scoped)
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<ISurveyService, SurveyService>();
    builder.Services.AddScoped<IQuestionService, QuestionService>();
    builder.Services.AddScoped<IResponseService, ResponseService>();
    builder.Services.AddScoped<IUserService, UserService>();

    // Register Telegram Bot Services
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

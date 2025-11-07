using Microsoft.OpenApi.Models;
// TODO: Uncomment when OpenTelemetry packages are added
// using OpenTelemetry.Exporter;
// using OpenTelemetry.Resources;
// using OpenTelemetry.Trace;
using PRFactory.Api.Middleware;
using Serilog;
using Serilog.Events;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// Configure Serilog
// ============================================================================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "PRFactory.Api")
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/prfactory-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ============================================================================
// Configure Services
// ============================================================================

// Add controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Add API Explorer and Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PRFactory API",
        Version = "v1",
        Description = "API for managing Jira-triggered automated development workflows with Claude AI",
        Contact = new OpenApiContact
        {
            Name = "PRFactory Team",
            Email = "support@prfactory.io"
        }
    });

    // Include XML comments for better API documentation
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Add security definition for webhook signature
    options.AddSecurityDefinition("WebhookSignature", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-Hub-Signature",
        Description = "HMAC-SHA256 signature for Jira webhook validation"
    });
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // Get allowed origins from configuration
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000", "http://localhost:5173" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ============================================================================
// Configure Entity Framework Core
// ============================================================================
// TODO: Add DbContext registration when PRFactory.Infrastructure is implemented
// builder.Services.AddDbContext<ApplicationDbContext>(options =>
// {
//     var connectionString = builder.Configuration.GetConnectionString("Database");
//     options.UseSqlite(connectionString);
//
//     if (builder.Environment.IsDevelopment())
//     {
//         options.EnableSensitiveDataLogging();
//         options.EnableDetailedErrors();
//     }
// });

// ============================================================================
// Configure HTTP Clients (Refit)
// ============================================================================
// TODO: Add Refit clients when PRFactory.Infrastructure is implemented
// builder.Services.AddRefitClient<IJiraClient>()
//     .ConfigureHttpClient(c =>
//     {
//         c.BaseAddress = new Uri(builder.Configuration["Jira:BaseUrl"]!);
//         c.DefaultRequestHeaders.Add("Authorization", $"Bearer {builder.Configuration["Jira:ApiToken"]}");
//     });

// ============================================================================
// Configure Agent Framework
// ============================================================================
// TODO: Add Microsoft.Agents.AI configuration when PRFactory.Infrastructure is implemented
// builder.Services.AddAgents(options =>
// {
//     options.UseCheckpointing = true;
//     options.CheckpointDirectory = Path.Combine(builder.Configuration["Workspace:BasePath"]!, "checkpoints");
// });

// ============================================================================
// Configure OpenTelemetry
// ============================================================================
var serviceName = "PRFactory.Api";
var serviceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

// TODO: AddOpenTelemetry requires OpenTelemetry NuGet package
// builder.Services.AddOpenTelemetry()
//     .WithTracing(tracerProviderBuilder =>
//     {
//         tracerProviderBuilder
//             .SetResourceBuilder(ResourceBuilder.CreateDefault()
//                 .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
//             .AddAspNetCoreInstrumentation(options =>
//             {
//                 options.RecordException = true;
//                 options.Filter = httpContext =>
//                 {
//                     // Don't trace health check endpoints
//                     return !httpContext.Request.Path.StartsWithSegments("/health");
//                 };
//             })
//             .AddHttpClientInstrumentation(options =>
//             {
//                 options.RecordException = true;
//             })
//             .AddEntityFrameworkCoreInstrumentation(options =>
//             {
//                 options.SetDbStatementForText = true;
//             })
//             .AddSource(serviceName);
//
//         // Configure exporters based on environment
//         var jaegerEndpoint = builder.Configuration["OpenTelemetry:JaegerEndpoint"];
//         if (!string.IsNullOrEmpty(jaegerEndpoint))
//         {
//             tracerProviderBuilder.AddJaegerExporter(options =>
//             {
//                 options.AgentHost = new Uri(jaegerEndpoint).Host;
//                 options.AgentPort = new Uri(jaegerEndpoint).Port;
//                 options.Protocol = JaegerExportProtocol.UdpCompactThrift;
//             });
//         }
//
//         if (builder.Environment.IsDevelopment())
//         {
//             tracerProviderBuilder.AddConsoleExporter();
//         }
//     });

// ============================================================================
// Configure Health Checks
// ============================================================================
builder.Services.AddHealthChecks();
// TODO: Add health check for database and external services
// .AddDbContextCheck<ApplicationDbContext>()
// .AddCheck<JiraHealthCheck>("jira")
// .AddCheck<ClaudeHealthCheck>("claude");

// ============================================================================
// Register Application Services
// ============================================================================
// TODO: Register repositories and services when PRFactory.Infrastructure is implemented
// builder.Services.AddScoped<ITicketRepository, TicketRepository>();
// builder.Services.AddScoped<IRepositoryRepository, RepositoryRepository>();
// builder.Services.AddScoped<ITenantRepository, TenantRepository>();
// builder.Services.AddScoped<IAgentOrchestrationService, AgentOrchestrationService>();

// ============================================================================
// Build Application
// ============================================================================
var app = builder.Build();

// ============================================================================
// Configure Middleware Pipeline
// ============================================================================

// Exception handling - must be first
app.UseExceptionHandlingMiddleware();

// Development-specific middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "PRFactory API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

// Request logging with Serilog
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent);
    };
});

// CORS
app.UseCors();

// HTTPS Redirection
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Webhook authentication (validates HMAC signatures)
app.UseJiraWebhookAuthentication();

// Authentication & Authorization (if needed later)
// app.UseAuthentication();
// app.UseAuthorization();

// Map controllers
app.MapControllers();

// Health checks
app.MapHealthChecks("/health");

// ============================================================================
// Database Migration (Development only)
// ============================================================================
if (app.Environment.IsDevelopment())
{
    // TODO: Auto-migrate database in development
    // using var scope = app.Services.CreateScope();
    // var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // await dbContext.Database.MigrateAsync();
}

// ============================================================================
// Start Application
// ============================================================================
try
{
    Log.Information("Starting PRFactory.Api");
    Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
    Log.Information("Configuration loaded from: {ConfigurationSource}",
        builder.Configuration.GetDebugView());

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using PRFactory.Api.Middleware;
using PRFactory.Infrastructure.Persistence;
using Serilog;
using Serilog.Events;
using System.Reflection;

// Default allowed CORS origins (static to avoid repeated allocations)
var defaultAllowedOrigins = new[] { "http://localhost:3000", "http://localhost:5173" };

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
            ?? defaultAllowedOrigins;

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ============================================================================
// Configure Identity
// ============================================================================
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// External authentication providers
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
            ?? "not-configured";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
            ?? "not-configured";
        options.CallbackPath = "/signin-google";
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.SaveTokens = true;
    })
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"]
            ?? "not-configured";
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"]
            ?? "not-configured";
        options.CallbackPath = "/signin-microsoft";
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.SaveTokens = true;
    });

// ============================================================================
// Configure Health Checks
// ============================================================================
builder.Services.AddHealthChecks();

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

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Health checks
app.MapHealthChecks("/health");

// ============================================================================
// Start Application
// ============================================================================
try
{
    Log.Information(
        "Starting PRFactory.Api - Environment: {Environment}, Configuration: {ConfigurationSource}",
        app.Environment.EnvironmentName,
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

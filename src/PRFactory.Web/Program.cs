using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using PRFactory.Infrastructure;
using PRFactory.Infrastructure.Agents;
using PRFactory.Infrastructure.Persistence;
using PRFactory.Web.BackgroundServices;
using PRFactory.Web.Hubs;
using PRFactory.Web.Middleware;
using PRFactory.Web.Services;
using Serilog;
using Serilog.Events;
using System.Reflection;

// Constants
const string NotConfiguredPlaceholder = "not-configured";
var defaultAllowedOrigins = new[] { "http://localhost:3000", "http://localhost:5173" };

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// LOGGING (Serilog)
// ============================================================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore.SignalR", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "PRFactory")
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/prfactory-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ============================================================
// INFRASTRUCTURE (Database, Repositories, Application Services)
// ============================================================
builder.Services.AddInfrastructure(builder.Configuration);

// ============================================================
// IDENTITY & AUTHENTICATION
// ============================================================
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// External authentication providers
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
            ?? NotConfiguredPlaceholder;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
            ?? NotConfiguredPlaceholder;
        options.CallbackPath = "/signin-google";
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.SaveTokens = true;
    })
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"]
            ?? NotConfiguredPlaceholder;
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"]
            ?? NotConfiguredPlaceholder;
        options.CallbackPath = "/signin-microsoft";
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.SaveTokens = true;
    });

// Configure authentication cookie for Blazor Server
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/auth/login";
    options.LogoutPath = "/api/auth/logout";
    options.AccessDeniedPath = "/auth/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Add authorization policies
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("RequireOwner", policy =>
        policy.RequireClaim("role", "Owner"));
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireClaim("role", "Owner", "Admin"));
});

// Add cascading authentication state for Blazor
builder.Services.AddCascadingAuthenticationState();

// ============================================================
// BLAZOR SERVER
// ============================================================
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSignalR();

// Register SignalR event broadcaster
builder.Services.AddScoped<PRFactory.Infrastructure.Events.IEventBroadcaster,
    SignalREventBroadcaster>();

// ============================================================
// API CONTROLLERS
// ============================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

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

// ============================================================
// CORS (for API endpoints)
// ============================================================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? defaultAllowedOrigins;
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ============================================================
// MAPSTER (DTO Mapping - Phase 5)
// ============================================================
PRFactory.Web.Mapping.MappingConfiguration.Configure();
builder.Services.AddSingleton(Mapster.TypeAdapterConfig.GlobalSettings);
builder.Services.AddScoped<MapsterMapper.IMapper, MapsterMapper.Mapper>();

// ============================================================
// WEB SERVICES (Facades for Blazor components)
// ============================================================
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IRepositoryService, RepositoryService>();
builder.Services.AddScoped<IWorkflowEventService, WorkflowEventService>();
builder.Services.AddScoped<IAgentPromptService, AgentPromptService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IErrorService, ErrorService>();
builder.Services.AddScoped<IAgentConfigurationService, AgentConfigurationService>();
builder.Services.AddScoped<IToastService, ToastService>();

// ============================================================
// BACKGROUND SERVICES (from Worker)
// ============================================================
// Configure Agent Host Options
builder.Services.Configure<AgentHostOptions>(
    builder.Configuration.GetSection("AgentHost"));

// Register Worker Services
builder.Services.AddScoped<IWorkflowResumeHandler, WorkflowResumeHandler>();

// Add Background Service
builder.Services.AddHostedService<AgentHostService>();

// Support Windows Service and systemd
if (OperatingSystem.IsWindows())
{
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "PRFactory Service";
    });
}
else if (OperatingSystem.IsLinux())
{
    builder.Services.AddSystemd();
}

// ============================================================
// HEALTH CHECKS
// ============================================================
builder.Services.AddHealthChecks();

// ============================================================
// BUILD APP
// ============================================================
var app = builder.Build();

// Seed demo data in Development environment
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbSeeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await dbSeeder.SeedAsync();
}

// ============================================================
// MIDDLEWARE PIPELINE
// ============================================================

// Exception handling - must be first
app.UseExceptionHandlingMiddleware();

// Development-specific middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "PRFactory API v1");
        options.RoutePrefix = "swagger"; // Serve Swagger UI at /swagger
    });
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
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

// Static files & routing
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// CORS (must be before Authentication)
app.UseCors();

// Jira webhook authentication middleware
app.UseJiraWebhookAuthentication();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// ============================================================
// ENDPOINT MAPPING
// ============================================================

// Health checks
app.MapHealthChecks("/health");

// API Controllers
app.MapControllers();

// Blazor & SignalR
app.MapBlazorHub();
app.MapHub<TicketHub>("/hubs/tickets");
app.MapFallbackToPage("/_Host");

// ============================================================
// RUN APP
// ============================================================
try
{
    Log.Information(
        "Starting PRFactory - Environment: {Environment}",
        app.Environment.EnvironmentName);
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

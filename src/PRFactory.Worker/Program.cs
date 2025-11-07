using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
// TODO: Uncomment when OpenTelemetry packages are added
// using OpenTelemetry.Resources;
// using OpenTelemetry.Trace;
// using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Events;
using PRFactory.Infrastructure.Agents;
using PRFactory.Worker;

// Create logger for startup
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting PRFactory Worker Service");

    var builder = Host.CreateApplicationBuilder(args);

    // Configure Serilog from appsettings.json
    // TODO: ReadFrom.Configuration requires Serilog.Settings.Configuration package
    builder.Services.AddSerilog((services, lc) => lc
        // .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "PRFactory.Worker")
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/worker-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30));

    // Add configuration
    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
        .AddEnvironmentVariables()
        .AddUserSecrets<Program>(optional: true);

    // Configure services
    ConfigureServices(builder.Services, builder.Configuration);

    // Configure OpenTelemetry
    ConfigureOpenTelemetry(builder.Services, builder.Configuration);

    // Add Worker Service
    builder.Services.AddHostedService<AgentHostService>();

    // Support Windows Service and systemd
    if (OperatingSystem.IsWindows())
    {
        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = "PRFactory Worker Service";
        });
    }
    else if (OperatingSystem.IsLinux())
    {
        builder.Services.AddSystemd();
    }

    var host = builder.Build();

    Log.Information("PRFactory Worker Service configured successfully");

    await host.RunAsync();

    Log.Information("PRFactory Worker Service stopped cleanly");
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "PRFactory Worker Service terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // Configure Agent Host Options
    services.Configure<AgentHostOptions>(
        configuration.GetSection("AgentHost"));

    // Register Infrastructure Services
    // TODO: These would come from PRFactory.Infrastructure
    // services.AddScoped<IAgentExecutionQueue, AgentExecutionQueue>();
    // services.AddScoped<IAgentGraphExecutor, AgentGraphExecutor>();
    // services.AddScoped<ICheckpointStore, CheckpointStore>();
    // services.AddScoped<ITicketRepository, TicketRepository>();

    // Register Worker Services
    services.AddScoped<IWorkflowResumeHandler, WorkflowResumeHandler>();

    // Register Database Context
    // services.AddDbContext<ApplicationDbContext>(options =>
    // {
    //     var connectionString = configuration.GetConnectionString("Database");
    //     options.UseSqlite(connectionString);
    //     options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    // });

    // Register Agent Framework Services
    // This would integrate with Microsoft.Agents.AI
    // services.AddAgentFramework(options =>
    // {
    //     options.ConfigureAgentGraph<WorkflowAgentGraph>();
    //     options.EnableCheckpointing();
    //     options.EnableTelemetry();
    // });

    // Register HTTP Clients for external services
    // TODO: AddHttpClient requires Microsoft.Extensions.Http package
    // services.AddHttpClient("Jira", client =>
    // {
    //     var jiraUrl = configuration["Jira:BaseUrl"];
    //     if (!string.IsNullOrEmpty(jiraUrl))
    //     {
    //         client.BaseAddress = new Uri(jiraUrl);
    //         client.DefaultRequestHeaders.Add("Accept", "application/json");
    //     }
    // });
    //
    // services.AddHttpClient("GitHub", client =>
    // {
    //     client.BaseAddress = new Uri("https://api.github.com");
    //     client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    //     client.DefaultRequestHeaders.Add("User-Agent", "PRFactory");
    // });
    //
    // services.AddHttpClient("Claude", client =>
    // {
    //     client.BaseAddress = new Uri("https://api.anthropic.com");
    //     client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    // });

    // Register Polly resilience policies
    // services.AddResiliencePipeline("default", builder =>
    // {
    //     builder
    //         .AddRetry(new RetryStrategyOptions
    //         {
    //             MaxRetryAttempts = 3,
    //             Delay = TimeSpan.FromSeconds(1),
    //             BackoffType = DelayBackoffType.Exponential
    //         })
    //         .AddTimeout(TimeSpan.FromMinutes(2));
    // });

    Log.Information("Services configured successfully");
}

static void ConfigureOpenTelemetry(IServiceCollection services, IConfiguration configuration)
{
    var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
    var enableConsoleExporter = configuration.GetValue<bool>("OpenTelemetry:EnableConsoleExporter");

    // TODO: AddOpenTelemetry requires OpenTelemetry NuGet package
    // services.AddOpenTelemetry()
    //     .ConfigureResource(resource => resource
    //         .AddService(
    //             serviceName: configuration["OpenTelemetry:ServiceName"] ?? "PRFactory.Worker",
    //             serviceVersion: configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0")
    //         .AddAttributes(new Dictionary<string, object>
    //         {
    //             ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
    //             ["host.name"] = Environment.MachineName
    //         }))
    //     .WithTracing(tracing =>
    //     {
    //         tracing
    //             .AddSource("PRFactory.*")
    //             .AddHttpClientInstrumentation(options =>
    //             {
    //                 options.RecordException = true;
    //                 options.EnrichWithHttpRequestMessage = (activity, request) =>
    //                 {
    //                     activity.SetTag("http.request.method", request.Method.ToString());
    //                 };
    //             })
    //             .AddEntityFrameworkCoreInstrumentation(options =>
    //             {
    //                 options.SetDbStatementForText = true;
    //                 options.EnrichWithIDbCommand = (activity, command) =>
    //                 {
    //                     activity.SetTag("db.query", command.CommandText);
    //                 };
    //             });
    //
    //         if (enableConsoleExporter)
    //         {
    //             tracing.AddConsoleExporter();
    //         }
    //
    //         if (!string.IsNullOrEmpty(otlpEndpoint))
    //         {
    //             tracing.AddOtlpExporter(options =>
    //             {
    //                 options.Endpoint = new Uri(otlpEndpoint);
    //             });
    //         }
    //     })
    //     .WithMetrics(metrics =>
    //     {
    //         metrics
    //             .AddMeter("PRFactory.*")
    //             .AddHttpClientInstrumentation()
    //             .AddRuntimeInstrumentation();
    //
    //         if (enableConsoleExporter)
    //         {
    //             metrics.AddConsoleExporter();
    //         }
    //
    //         if (!string.IsNullOrEmpty(otlpEndpoint))
    //         {
    //             metrics.AddOtlpExporter(options =>
    //             {
    //                 options.Endpoint = new Uri(otlpEndpoint);
    //             });
    //         }
    //     });

    Log.Information("OpenTelemetry configured with OTLP endpoint: {Endpoint}", otlpEndpoint ?? "None");
}

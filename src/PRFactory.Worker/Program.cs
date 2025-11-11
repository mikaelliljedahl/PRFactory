using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    builder.Services.AddSerilog((services, lc) => lc
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

    // Register Worker Services
    services.AddScoped<IWorkflowResumeHandler, WorkflowResumeHandler>();

    Log.Information("Services configured successfully");
}

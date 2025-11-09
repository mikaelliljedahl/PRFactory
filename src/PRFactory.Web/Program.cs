using PRFactory.Infrastructure;
using PRFactory.Web.Hubs;
using PRFactory.Web.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/prfactory-web-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add Radzen components
builder.Services.AddRadzenComponents();

// Add SignalR
builder.Services.AddSignalR();

// Register Infrastructure services (includes DbContext, repositories, application services, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Register SignalR event broadcaster
builder.Services.AddScoped<PRFactory.Infrastructure.Events.IEventBroadcaster,
    SignalREventBroadcaster>();

// Register Web UI services (facades for Blazor components)
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IRepositoryService, RepositoryService>();
builder.Services.AddScoped<IWorkflowEventService, WorkflowEventService>();
builder.Services.AddScoped<IAgentPromptService, AgentPromptService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IErrorService, ErrorService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapHub<TicketHub>("/hubs/tickets");
app.MapFallbackToPage("/_Host");

try
{
    Log.Information("Starting PRFactory Web UI");
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

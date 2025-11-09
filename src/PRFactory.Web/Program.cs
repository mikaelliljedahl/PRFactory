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

// Add SignalR
builder.Services.AddSignalR();

// Register SignalR event broadcaster
builder.Services.AddScoped<PRFactory.Infrastructure.Events.IEventBroadcaster,
    SignalREventBroadcaster>();

// Register Infrastructure services (repositories, application services, agents, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Register web layer facade services
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IRepositoryService, RepositoryService>();
builder.Services.AddScoped<IWorkflowEventService, WorkflowEventService>();
builder.Services.AddScoped<IAgentPromptService, AgentPromptService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IErrorService, ErrorService>();

// Register Toast notification service
builder.Services.AddScoped<IToastService, ToastService>();

var app = builder.Build();

// Seed demo data in Development environment
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbSeeder = scope.ServiceProvider.GetRequiredService<PRFactory.Infrastructure.Persistence.DbSeeder>();
    await dbSeeder.SeedAsync();
}

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

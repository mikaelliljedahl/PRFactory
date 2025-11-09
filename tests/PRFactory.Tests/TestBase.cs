using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PRFactory.Infrastructure.Persistence;

namespace PRFactory.Tests;

/// <summary>
/// Base class for all tests providing common setup and utilities.
/// </summary>
public abstract class TestBase : IDisposable
{
    protected IServiceProvider ServiceProvider { get; }
    protected ApplicationDbContext DbContext { get; }

    protected TestBase()
    {
        var services = new ServiceCollection();

        // Configure in-memory database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        ServiceProvider = services.BuildServiceProvider();
        DbContext = ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public virtual void Dispose()
    {
        DbContext?.Dispose();
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

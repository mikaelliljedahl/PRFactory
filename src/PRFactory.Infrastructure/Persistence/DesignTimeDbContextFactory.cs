using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PRFactory.Infrastructure.Persistence.Encryption;

namespace PRFactory.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for creating ApplicationDbContext instances.
/// Used by EF Core tools (migrations, etc.) when the application is not running.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // For design-time, use a dummy encryption key
        // In production, this should come from configuration
        var dummyEncryptionKey = Convert.ToBase64String(new byte[32]); // 256-bit key
        var encryptionService = new AesEncryptionService(
            dummyEncryptionKey,
            NullLogger<AesEncryptionService>.Instance);

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Use SQLite for the database
        optionsBuilder.UseSqlite("Data Source=prfactory.db");

        return new ApplicationDbContext(
            optionsBuilder.Options,
            encryptionService,
            NullLogger<ApplicationDbContext>.Instance);
    }
}

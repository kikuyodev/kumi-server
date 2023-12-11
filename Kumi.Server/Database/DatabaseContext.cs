using System.Configuration;
using Kumi.Server.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Kumi.Server.Database;

public class DatabaseContext : DbContext
{
    public DbSet<Account> Accounts { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string? connection_string = $"Host={ConfigurationManager.AppSettings["database:host"] ?? "localhost"};" +
                                    $"Port={ConfigurationManager.AppSettings["database:port"] ?? "5432"};" +
                                    $"Database={ConfigurationManager.AppSettings["database:database"] ?? "kumi"};" +
                                    $"Username={ConfigurationManager.AppSettings["database:username"] ?? "postgres"};" +
                                    $"Password={ConfigurationManager.AppSettings["database:password"] ?? "postgres"}";

        optionsBuilder.UseNpgsql(connection_string);
    }
}

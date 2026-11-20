using Microsoft.EntityFrameworkCore;
using NewsAggregator.Web.Models;

namespace NewsAggregator.Web.Data;

// Inherits from DbContext, which is the core class in Entity Framework Core
// responsible for managing database connections and tracking changes.
public class AppDbContext : DbContext
{
    // The constructor receives configuration options (like the SQLite file path)
    // via Dependency Injection and passes them to the base DbContext class. --> Depends on config in Program.cs
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Represents the "Articles" table in the database.
    // Using Expression-Bodied Property (=> Set<Article>()) avoids compiler warnings
    // about uninitialized non-nullable properties.
    public DbSet<Article> Articles => Set<Article>();
    /* Alternative Version: 
    public DbSet<Article> Articles
    {
        get
        {
            return Set<Article>();
        }
    }*/
}
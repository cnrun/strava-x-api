using Microsoft.EntityFrameworkCore;

// Announcing Entity Framework Core 3.0
//      https://devblogs.microsoft.com/dotnet/announcing-ef-core-3-0-and-ef-6-3-general-availability/
// Entity Framework Core
//      https://docs.microsoft.com/de-de/ef/core/
// Erste Schritte mit EF Core
//      https://docs.microsoft.com/de-de/ef/core/get-started/index?tabs=netcore-cli
namespace Prototype.Model
{    
    public class StravaXApiContext : DbContext
    {
        public DbSet<ActivityShort> ActivityShortDB { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=StravaXApi.db");
    }
}

using System;
using Microsoft.EntityFrameworkCore;

// Announcing Entity Framework Core 3.0
//      https://devblogs.microsoft.com/dotnet/announcing-ef-core-3-0-and-ef-6-3-general-availability/
// Entity Framework Core
//      https://docs.microsoft.com/de-de/ef/core/
// Erste Schritte mit EF Core
//      https://docs.microsoft.com/de-de/ef/core/get-started/index?tabs=netcore-cli
// Add Entity Framework:
// 1a) dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 3.0.0
// 1b) dotnet add package Microsoft.EntityFrameworkCore.Sqlite
// 2)dotnet tool install --global dotnet-ef
//      Sie können das Tool über den folgenden Befehl aufrufen: dotnet-ef
//      Das Tool "dotnet-ef" (Version 3.0.0) wurde erfolgreich installiert.
// 3) dotnet add package Microsoft.EntityFrameworkCore.Design
//      ...
//      log  : Wiederherstellung in "1,83 sec" für "/Users/ericlouvard/Documents/Projects/strava-x-api/Prototype/Prototype.csproj" abgeschlossen.
// 4) dotnet ef migrations add InitialCreate
//      Done. To undo this action, use 'ef migrations remove'
// 5) dotnet ef database update
//      Executing DbCommand [Parameters=[], CommandType='Text', CommandTimeout='30']
//      PRAGMA journal_mode = 'wal';
//      Executing DbCommand [Parameters=[], CommandType='Text', CommandTimeout='30']
//      CREATE TABLE "__EFMigrationsHistory" (
//          "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
//          "ProductVersion" TEXT NOT NULL
//      );
//      Executing DbCommand [Parameters=[], CommandType='Text', CommandTimeout='30']
//      SELECT COUNT(*) FROM "sqlite_master" WHERE "name" = '__EFMigrationsHistory' AND "type" = 'table';
//      Executing DbCommand [Parameters=[], CommandType='Text', CommandTimeout='30']
//      SELECT "MigrationId", "ProductVersion"
//      FROM "__EFMigrationsHistory"
//      ORDER BY "MigrationId";
//      Applying migration '20191028211236_InitialCreate'.
//      Executing DbCommand [Parameters=[], CommandType='Text', CommandTimeout='30']
//      CREATE TABLE "ActivityShortDB" (
//          "ActivityId" TEXT NOT NULL CONSTRAINT "PK_ActivityShortDB" PRIMARY KEY,
//          "ActivityTyp        e" INTEGER NOT NULL,
//          "ActivityDate" TEXT NOT NULL,
//          "ActivityTitle" TEXT NULL,
//          "ActivityImageMapUrl" TEXT NULL,
//          "AthleteId" TEXT NULL
//      );
//      Executing DbCommand [Parameters=[], CommandType='Text', CommandTimeout='30']
//      INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
//      VALUES ('20191028211236_InitialCreate', '3.0.0');
//      Done.

namespace Prototype.Model
{    
    public class StravaXApiContext : DbContext
    {
        public DbSet<ActivityShort> ActivityShortDB { get; set; }
        public DbSet<AthleteShort> AthleteShortDB { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=StravaXApi.db").EnableSensitiveDataLogging();
            // => options.UseSqlServer(Environment.GetEnvironmentVariable("CONNECTION_STRING"));
            // => options.UseSqlServer(System.Configuration.Configuration["Data:db:ConnectionString"]);            
    }
}

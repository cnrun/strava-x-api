using System;
using System.Linq;
using Prototype.Model;
using Microsoft.EntityFrameworkCore;

namespace Prototype.Tools
{    
    public class DbClone
    {
        static public int DoClone(string[] args)
        {
            DbContextOptions optionsSrc = new DbContextOptionsBuilder().UseSqlite("Data Source=data/StravaXApi.db").Options;
            DbContextOptions optionsDst = new DbContextOptionsBuilder().UseSqlServer("Server=tcp:strava-x-api-gen1.database.windows.net,1433;Initial Catalog=StravaActivityDB;Persist Security Info=False;User ID=EricLouvard;Password=QxeGjvMqwA5NB4WI;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;").Options;
            using (StravaXApiContext DbSrc = new StravaXApiContext(optionsSrc))
            {
                Console.WriteLine($"SRC: Queries stored {DbSrc.ActivityQueriesDB.Count()}");
                Console.WriteLine($"SRC: Activities stored {DbSrc.ActivityShortDB.Count()}");
                var al = DbSrc.ActivityShortDB.Select(a => a.AthleteId).Distinct();
                Console.WriteLine($"SRC: Athletes {al.Count()} from {DbSrc.AthleteShortDB.Count()}");
                using (StravaXApiContext DbDst = new StravaXApiContext(optionsDst))
                {
                    // https://stackoverflow.com/questions/38238043/how-and-where-to-call-database-ensurecreated-and-database-migrate
                    DbDst.Database.EnsureCreated();
                    Console.WriteLine($"DST: Queries stored {DbDst.ActivityQueriesDB.Count()}");
                    Console.WriteLine($"DST: Activities stored {DbDst.ActivityShortDB.Count()}");
                    var AlDst = DbDst.ActivityShortDB.Select(a => a.AthleteId).Distinct();
                    Console.WriteLine($"DST: Athletes {AlDst.Count()} from {DbDst.AthleteShortDB.Count()}");
                }
            }
            return 0;
        }
    }
}

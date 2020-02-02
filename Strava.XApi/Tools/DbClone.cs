using System;
using System.Linq;
using Strava.XApi.Model;
using Microsoft.EntityFrameworkCore;
using NDesk.Options;

namespace Strava.XApi.Tools
{    
    public class DbClone
    {
        static public int DoClone(string[] args)
        {
            String SrcConnectionString = null;
            String DestConnectionString = null;
            var p = new OptionSet () {
                { "s|source=",   s => { SrcConnectionString=s; } },
                { "d|destination=",   d => { DestConnectionString=d; } },
            };
            p.Parse(args);
            if (SrcConnectionString==null)
            {
                p.WriteOptionDescriptions(Console.Out);
                throw new ArgumentException("missing source");    
            }
            if (DestConnectionString==null)
            {
                p.WriteOptionDescriptions(Console.Out);
                throw new ArgumentException("missing destination");    
            }

            Console.WriteLine($"SRC: {SrcConnectionString}");
            Console.WriteLine($"DST: {DestConnectionString}");

            DbContextOptions optionsSrc;
            if (SrcConnectionString.StartsWith("Data Source"))
                optionsSrc = new DbContextOptionsBuilder().UseSqlite(SrcConnectionString).Options;
            else
                optionsSrc = new DbContextOptionsBuilder().UseSqlServer(SrcConnectionString).Options;

            DbContextOptions optionsDst;
            if (DestConnectionString.StartsWith("Data Source"))
                optionsDst = new DbContextOptionsBuilder().UseSqlite(DestConnectionString).Options;
            else
                optionsDst = new DbContextOptionsBuilder().UseSqlServer(DestConnectionString).Options;

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

                    Console.WriteLine("read athletes");
                    foreach(AthleteShort ath in DbSrc.AthleteShortDB)
                    {
                        DbDst.AthleteShortDB.Add(ath);
                    }
                    Console.WriteLine("save athletes");
                    DbDst.SaveChanges();

                    Console.WriteLine("read activities");
                    int i=0;
                    int totalCount=DbSrc.ActivityShortDB.Count();
                    int destLen = DbDst.ActivityShortDB.Count();
                    foreach(ActivityShort act in DbSrc.ActivityShortDB)
                    {
                        // skip as much activities as already present.
                        // Only done because the first imports try has broked, but
                        // a better system should be developped in the future.
                        // if (i++<destLen)
                        // {
                        //     continue;
                        // }
                        if (++i%100==0)
                            Console.WriteLine($"activities {i}/{totalCount}");
                        DbDst.ActivityShortDB.Add(act);
                    }
                    Console.WriteLine("save activities");
                    DbDst.SaveChanges();

                    Console.WriteLine("read queries");
                    i=0;
                    totalCount=DbSrc.ActivityQueriesDB.Count();
                    foreach(ActivityRangeQuery arq in DbSrc.ActivityQueriesDB)
                    {
                        DbDst.ActivityQueriesDB.Add(arq);
                        i++;
                        if (i%100==0)
                            Console.WriteLine($"queries {i}/{totalCount}");
                    }
                    Console.WriteLine("save queries");
                    DbDst.SaveChanges();
                }
            }
            return 0;
        }
    }
}

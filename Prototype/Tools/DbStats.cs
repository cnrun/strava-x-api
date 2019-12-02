using System;
using System.Linq;
using Prototype.Model;
using System.Collections.Generic;

namespace Prototype.Tools
{    
    public class DbStats
    {
        static public int WriteState(string[] args)
        {
            int ret = -1;
            try
            {
                using (StravaXApiContext db = new StravaXApiContext())
                {
                    Console.WriteLine($"Queries stored {db.ActivityQueriesDB.Count()}");
                    Console.WriteLine($"Activities stored {db.ActivityShortDB.Count()}");
                    var al = db.ActivityShortDB.Select(a => a.AthleteId).Distinct();
                    Console.WriteLine($"Athletes {al.Count()} from {db.AthleteShortDB.Count()}");
                    /*
                    foreach(var aId in al)
                    {
                        AthleteShort ath = db.AthleteShortDB.Find(aId);
                        // for format: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/interpolated
                        if (ath != null)
                        {                            
                            Console.WriteLine($" Activities:{db.ActivityShortDB.Count(a => a.AthleteId==aId),6} for {ath.AthleteId,9} {ath.AthleteName,-40} {ath.AthleteLocation}");
                        }
                        else
                        {
                            Console.WriteLine($" Activities:{db.ActivityShortDB.Count(a => a.AthleteId==aId),6} for {aId,9}");
                        }
                    }
                    */
                    foreach(ActivityRangeQuery arq in db.ActivityQueriesDB)
                    {
                        Console.WriteLine($"{arq}");
                        break;
                    }
                    var status = db.ActivityQueriesDB.Select(a => a.Status).Distinct();
                    Console.WriteLine($"Queries:");
                    foreach(var st in status)
                    {
                        Console.WriteLine($" {st} {db.ActivityQueriesDB.Count(a => a.Status==st)}");
                    }
                    Console.WriteLine($"Activity types Σ :{db.ActivityShortDB.Count()}");
                    var ActivityTypes = db.ActivityShortDB.Select(a => a.ActivityType).Distinct();
                    foreach(var aType in ActivityTypes)
                    {
                        Console.WriteLine($" {aType,18} {db.ActivityShortDB.Count(a => a.ActivityType==aType),6}");
                    }
                    Console.WriteLine($"All athletes with {ActivityType.BackcountrySki}");
                    var Activity4Type = db.ActivityShortDB.Where(a => a.ActivityType==ActivityType.BackcountrySki).Select(a => a.AthleteId).Distinct();
                    foreach(var A4Type in Activity4Type)
                    {
                        var count = db.ActivityShortDB.Where(a => a.ActivityType==ActivityType.BackcountrySki).Where(a => a.AthleteId==A4Type).Count();
                        var athlete = db.AthleteShortDB.Find(A4Type);
                        Console.WriteLine($" {athlete?.AthleteName,30} : {A4Type,8} ({count})");
                    }
                    /*
                    Console.WriteLine($"All athletes with {ActivityType.Run}");
                    Activity4Type = db.ActivityShortDB.Where(a => a.ActivityType==ActivityType.Run).Select(a => a.AthleteId).Distinct();
                    foreach(var A4Type in Activity4Type)
                    {
                        var count = db.ActivityShortDB.Where(a => a.ActivityType==ActivityType.Run).Where(a => a.AthleteId==A4Type).Count();
                        var athlete = db.AthleteShortDB.Find(A4Type);
                        Console.WriteLine($" {athlete?.AthleteName,30} : {A4Type,8} ({count})");
                    }
                    */
                }
                ret = 0 ;
            }
            catch(Exception e)
            {
                Console.WriteLine($"ERROR:{e.ToString()}"); 
                ret = 1; 
            }
            return ret;
        }
    }
}

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
                    foreach(var aId in al)
                    {
                        AthleteShort ath = db.AthleteShortDB.Find(aId);
                        // for format: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/interpolated
                        Console.WriteLine($" Activities:{db.ActivityShortDB.Count(a => a.AthleteId==aId),6} for {ath.AthleteId,9} {ath.AthleteName,-40} {ath.AthleteLocation}");
                    }
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

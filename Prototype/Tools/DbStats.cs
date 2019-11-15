using System;
using System.Threading;
using System.Linq;
using OpenQA.Selenium;
using Prototype.Model;
using System.Collections.Generic;

namespace Prototype.Tools
{    
    public class DbStats
    {
        static public void WriteState(string[] args)
        {
            Console.WriteLine("Create range queries.");
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
        }
    }
}

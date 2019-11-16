using System;
using System.Linq;
using Prototype.Model;

namespace Prototype.Tools
{    
    public class QueriesGenerator
    {
        static internal void WriteQueriesForAthletes(StravaXApi stravaXApi, string[] args)
        {
            Console.WriteLine("Create range queries.");
            using (StravaXApiContext db = new StravaXApiContext())
            {
                stravaXApi.signIn();
                try
                {
                    var AllAthletes = db.AthleteShortDB;
                    foreach(AthleteShort Athlete in AllAthletes)
                    {
                        Console.WriteLine($"Athlete:{Athlete}");
                        try
                        {
                            WriteQueriesForAthlete(stravaXApi, db, Athlete.AthleteId);
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine($"SKIP:{Athlete.AthleteId} {e.ToString()}");  
                        }
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine($"ERROR:{e.ToString()}");  
                }
                finally
                {
                    stravaXApi.Dispose();
                }
            }

        }

        static void WriteQueriesForAthlete(StravaXApi stravaXApi, StravaXApiContext db, string AthleteId)
        {
            DateTime FirstActivityDate = stravaXApi.getActivityRange(AthleteId);
            System.Console.WriteLine($"First activity at {FirstActivityDate.Year}/{FirstActivityDate.Month}");                    
            DateTime Now = DateTime.Now;

            int FromYear=FirstActivityDate.Year;
            int FromMonth=FirstActivityDate.Month;
            int ToYear=Now.Year;
            int ToMonth=Now.Month;
            Console.WriteLine($"Enterred queries: {db.ActivityQueriesDB.Count()}");
            // first year
            for(int month=FromMonth;month<=12;month++)
            {
                AddQuery(db, AthleteId, new DateTime(FromYear,month,1), new DateTime(FromYear,month,1).AddMonths(1).AddDays(-1));
            }
            // all years after
            for(int year=FromYear+1;year<=ToYear-1;year++)
            {
                for(int month=01;month<=12;month++)
                {
                    AddQuery(db, AthleteId, new DateTime(year,month,1), new DateTime(year,month,1).AddMonths(1).AddDays(-1));
                }
            }
            // last year
            for(int month=01;month<=ToMonth;month++)
            {
                // from first day of month to last day of the month
                AddQuery(db, AthleteId, new DateTime(ToYear,month,1), new DateTime(ToYear,month,1).AddMonths(1).AddDays(-1));
            }
            Console.WriteLine($"Enterred queries: {db.ActivityQueriesDB.Count()}");
        }
        static private void AddQuery(StravaXApiContext db, String AthleteId, DateTime DateFrom, DateTime DateTo)
        {
            if (db.ActivityQueriesDB.Find(AthleteId,DateFrom,DateTo)==null)
            {
                ActivityRangeQuery query = new ActivityRangeQuery();
                query.AthleteId=AthleteId;
                query.DateFrom=DateFrom;
                query.DateTo=DateTo;
                db.ActivityQueriesDB.Add(query);
                db.SaveChanges();
                Console.WriteLine($"add query for {query}");
            }            
        }
    }
}

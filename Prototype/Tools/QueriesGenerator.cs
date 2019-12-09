using System;
using System.Linq;
using Prototype.Model;
using System.Collections.Generic;
using System.Diagnostics;

namespace Prototype.Tools
{    
    public class QueriesGenerator
    {
        static internal int WriteQueriesForAthletes(StravaXApi stravaXApi)
        {
            int ret = -1 ;
            Console.WriteLine("Create range queries.");
            using (StravaXApiContext db = new StravaXApiContext())
            {
                stravaXApi.signIn();
                try
                {
                    // https://docs.microsoft.com/en-us/ef/ef6/querying/
                    // First retrieve all query objects to avoid "New transaction is not allowed because there are other threads running in the session."
                    // Not best praxis but enought for Prototype.
                    IList<AthleteShort> AllAthletes = db.AthleteShortDB.ToList();
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();
                    foreach(AthleteShort Athlete in AllAthletes)
                    {
                        TimeSpan ts = stopWatch.Elapsed;
                        try
                        {
                            WriteQueriesForAthlete(stravaXApi, db, Athlete.AthleteId);
                            Console.WriteLine($"Athlete:{Athlete} run since:{ts.TotalSeconds}s");
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine($"SKIP:{Athlete.AthleteId} {e.ToString()}");  
                        }
                        db.SaveChanges();
                    }
                    ret = 0 ;
                }
                catch(Exception e)
                {
                    Console.WriteLine($"ERROR:{e.ToString()}");  
                    ret = -1;
                }
                finally
                {
                    stravaXApi.Dispose();
                }
            }
            return ret;
        }

        static void WriteQueriesForAthlete(StravaXApi stravaXApi, StravaXApiContext db, string AthleteId)
        {
            DateTime Now = DateTime.Now;

            int ToYear=Now.Year;
            int ToMonth=Now.Month;
            //
            // Verify if queries have to be generated
            //

            // Retrieve all entered quries or the wanted athlete.
            List<ActivityRangeQuery> queriesForPatient = db.ActivityQueriesDB.Where(a => a.AthleteId==AthleteId).OrderBy(a => a.DateFrom).ToList();
            int FromYear;
            int FromMonth;

            // retrieve first and last date
            DateTime minDT = queriesForPatient.First().DateFrom;
            DateTime maxDT = queriesForPatient.Last().DateFrom;
            int UpdateNeed = 0;
            // If we already have an entry, we assume that the entry contains the first activity date, as it is expensive to retrieve its value with Selenium.
            if (queriesForPatient.Count==0)
            {
                // Retrieve the first activity date with selenium
                DateTime FirstActivityDate = stravaXApi.getActivityRange(AthleteId);
                System.Console.WriteLine($"First activity at {FirstActivityDate.Year}/{FirstActivityDate.Month}");                    
                FromYear=FirstActivityDate.Year;
                FromMonth=FirstActivityDate.Month;
            }
            else
            {
                FromYear=minDT.Year;
                FromMonth=minDT.Month;
            }

            // an update is needed if the wanted range is not included in saved one.
            if (new DateTime(FromYear,FromMonth,1) < minDT)
            {
                UpdateNeed |= 1;
            }
            if (new DateTime(ToYear,ToMonth,1) > maxDT)
            {
                UpdateNeed |= 2;
            }

            // Compare the theoric count between both date with the entered one.
            int TotalWantedQueries;
            if (FromYear==ToYear)
            {
                TotalWantedQueries=Math.Abs(FromMonth-ToMonth)+1;
            }
            else
            {
                TotalWantedQueries=12-FromMonth+1;
                int NumberFullYears=ToYear-FromYear-1;
                if (NumberFullYears>0)
                    TotalWantedQueries+=12*NumberFullYears;
                TotalWantedQueries=ToMonth;                
            }
            if (TotalWantedQueries>queriesForPatient.Count)
            {
                UpdateNeed |= 3;
            }
            
            if (UpdateNeed==0)
            {
                Console.WriteLine($"SKIP ({UpdateNeed}) queries wanted:{TotalWantedQueries}/enterred:{queriesForPatient.Count}/total:{db.ActivityQueriesDB.Count()}");
            }
            else
            {
                Console.WriteLine($"queries wanted:{TotalWantedQueries}/enterred:{queriesForPatient.Count}/total:{db.ActivityQueriesDB.Count()}");
                if (FromYear==ToYear)
                {
                    for(int month=FromMonth;month<=ToMonth;month++)
                    {
                        AddQuery(db, AthleteId, new DateTime(FromYear,month,1), new DateTime(FromYear,month,1).AddMonths(1).AddDays(-1));
                    }
                }
                else
                {
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
                }
            }
            queriesForPatient = db.ActivityQueriesDB.Where(a => a.AthleteId==AthleteId).OrderBy(a => a.DateFrom).ToList();
            Console.WriteLine($"✅ wanted:{TotalWantedQueries}/enterred:{queriesForPatient.Count}/total:{db.ActivityQueriesDB.Count()}");
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
                Console.WriteLine($"add query for {query}");
            }            
        }
    }
}

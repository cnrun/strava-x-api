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
            Console.WriteLine($"queries enterred:{queriesForPatient.Count}/total:{db.ActivityQueriesDB.Count()}");
            if (FromYear==ToYear)
            {
                for(int month=FromMonth;month<=ToMonth;month++)
                {
                    AddQuery(db, AthleteId, new DateTime(FromYear,month,1), new DateTime(FromYear,month,1).AddMonths(1).AddDays(-1), queriesForPatient);
                }
            }
            else
            {
                // first year
                for(int month=FromMonth;month<=12;month++)
                {
                    AddQuery(db, AthleteId, new DateTime(FromYear,month,1), new DateTime(FromYear,month,1).AddMonths(1).AddDays(-1), queriesForPatient);
                }
                // all years after
                for(int year=FromYear+1;year<=ToYear-1;year++)
                {
                    for(int month=01;month<=12;month++)
                    {
                        AddQuery(db, AthleteId, new DateTime(year,month,1), new DateTime(year,month,1).AddMonths(1).AddDays(-1), queriesForPatient);
                    }
                }
                // last year
                for(int month=01;month<=ToMonth;month++)
                {
                    // from first day of month to last day of the month
                    AddQuery(db, AthleteId, new DateTime(ToYear,month,1), new DateTime(ToYear,month,1).AddMonths(1).AddDays(-1), queriesForPatient);
                }
            }

            queriesForPatient = db.ActivityQueriesDB.Where(a => a.AthleteId==AthleteId).OrderBy(a => a.DateFrom).ToList();
            Console.WriteLine($"✅ enterred:{queriesForPatient.Count}/total:{db.ActivityQueriesDB.Count()}");
        }
        static private void AddQuery(StravaXApiContext db, String AthleteId, DateTime DateFrom, DateTime DateTo, List<ActivityRangeQuery> queriesForPatient)
        {
            if (!isDateInList(DateFrom,queriesForPatient))
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
        static private bool isDateInList(DateTime DateFrom, List<ActivityRangeQuery> queriesForPatient)
        {
            bool ret = false;
            foreach(ActivityRangeQuery aqr in queriesForPatient)
            {
                ret = aqr.DateFrom==DateFrom;
                if (ret) break;
            }
            return ret;
        }
    }
}

using System;
using System.Linq;
using Strava.XApi.Model;
using System.Collections.Generic;
using System.Diagnostics;

namespace Strava.XApi.Tools
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
                    // Not best praxis but enought for Strava.XApi.
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
                        catch(PrivateAthleteException e)
                        {
                            // TODO AB#27 athlete should be marked as private to avoid second visit.
                            // create a dummy Query to prevent next search
                            ActivityRangeQuery arq = new ActivityRangeQuery();
                            arq.AthleteId=Athlete.AthleteId;
                            arq.DateFrom=new DateTime(2019,12,01);
                            arq.DateTo=new DateTime(2019,12,31);
                            arq.Status=QueryStatus.Done;
                            arq.Message=$"private athlete {Athlete.AthleteId}";
                            arq.StatusChanged=DateTime.Now;
                            db.ActivityQueriesDB.Add(arq);
                            Console.WriteLine($"SKIP: private athlete {Athlete.AthleteId} {e.Message}");  
                        }
                        catch(TooManyStravaRequestException e)
                        {
                            Console.WriteLine($"Too Many Queries detected {e.Message} need to wait some hours");                            
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
        private static DateTime lastStravaHttpRequest=DateTime.Now;

        static void WriteQueriesForAthlete(StravaXApi stravaXApi, StravaXApiContext db, string AthleteId)
        {
            DateTime Now = DateTime.Now;
            lastStravaHttpRequest=DateTime.Now;
            int ToYear=Now.Year;
            int ToMonth=Now.Month;
            //
            // Verify if queries have to be generated
            //

            // Retrieve all entered queries or the wanted athlete.
            List<ActivityRangeQuery> queriesForAthlete = db.ActivityQueriesDB.Where(a => a.AthleteId==AthleteId).OrderBy(a => a.DateFrom).ToList();
            int FromYear;
            int FromMonth;

            // If we already have an entry, we assume that the entry contains the first activity date, as it is expensive to retrieve its value with Selenium.
            if (queriesForAthlete.Count==0)
            {
                // Retrieve the first activity date with selenium.
                // may throw PrivateAthleteException.

                // be sure of a one seconde intervall between to requests
                int dt=DateTime.Now.Millisecond-lastStravaHttpRequest.Millisecond;
                if (dt<2000)
                {
                    System.Threading.Tasks.Task.Delay(dt).Wait();
                }

                DateTime FirstActivityDate = stravaXApi.getActivityRange(AthleteId);
                lastStravaHttpRequest=DateTime.Now;
                
                System.Console.WriteLine($"First activity at {FirstActivityDate.Year}/{FirstActivityDate.Month}");                    
                FromYear=FirstActivityDate.Year;
                FromMonth=FirstActivityDate.Month;
            }
            else
            {
                // retrieve first and last date
                DateTime minDT = queriesForAthlete.First().DateFrom;
                // DateTime maxDT = queriesForPatient.Last().DateFrom;
                FromYear=minDT.Year;
                FromMonth=minDT.Month;
            }
            Console.WriteLine($"queries enterred:{queriesForAthlete.Count}/total:{db.ActivityQueriesDB.Count()}");
            if (FromYear==ToYear)
            {
                for(int month=FromMonth;month<=ToMonth;month++)
                {
                    AddQuery(db, AthleteId, new DateTime(FromYear,month,1), new DateTime(FromYear,month,1).AddMonths(1).AddDays(-1), queriesForAthlete);
                }
            }
            else
            {
                // first year
                for(int month=FromMonth;month<=12;month++)
                {
                    AddQuery(db, AthleteId, new DateTime(FromYear,month,1), new DateTime(FromYear,month,1).AddMonths(1).AddDays(-1), queriesForAthlete);
                }
                // all years after
                for(int year=FromYear+1;year<=ToYear-1;year++)
                {
                    for(int month=01;month<=12;month++)
                    {
                        AddQuery(db, AthleteId, new DateTime(year,month,1), new DateTime(year,month,1).AddMonths(1).AddDays(-1), queriesForAthlete);
                    }
                }
                // last year
                for(int month=01;month<=ToMonth;month++)
                {
                    // from first day of month to last day of the month
                    AddQuery(db, AthleteId, new DateTime(ToYear,month,1), new DateTime(ToYear,month,1).AddMonths(1).AddDays(-1), queriesForAthlete);
                }
            }

            int qCount = db.ActivityQueriesDB.Where(a => a.AthleteId==AthleteId).Count();
            Console.WriteLine($"✅ enterred:{qCount}/total:{db.ActivityQueriesDB.Count()}");
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

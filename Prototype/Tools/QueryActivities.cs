using System;
using System.Linq;
using Prototype.Model;
using System.Collections.Generic;
using System.IO;

namespace Prototype.Tools
{    
    public class QueryActivities
    {
        static internal int SendQueriesForActivities(StravaXApi stravaXApi, string[] args)
        {
            int ret = -1;
            Console.WriteLine("Query activities.");
            using (StravaXApiContext db = new StravaXApiContext())
            {
                stravaXApi.signIn();
                int Count=0;
                Boolean KeepRunning=true;
                int ErrorCountConsecutive=0;
                int ErrorCount=0;
                // https://docs.microsoft.com/en-us/ef/ef6/querying/
                // First retrieve all query objects to avoid "New transaction is not allowed because there are other threads running in the session."
                // Not best praxis but enought for Prototype.
                IList<ActivityRangeQuery> queries = db.ActivityQueriesDB.Where(a => a.Status==QueryStatus.Created).ToList();
                foreach(ActivityRangeQuery arq in queries)
                {
                    try
                    {
                        arq.Status=QueryStatus.Run;
                        arq.StatusChanged=DateTime.Now;
                        db.SaveChanges();
                        var ActivitiesList = stravaXApi.getActivities(arq.AthleteId,$"{arq.DateFrom.Year:D4}",$"{arq.DateFrom.Month:D2}");
                        foreach(ActivityShort ActivityShort in ActivitiesList)
                        {
                            // Console.WriteLine($"JSON={ActivityShort.SerializePrettyPrint(ActivityShort)}");
                            if (db.ActivityShortDB.Find(ActivityShort.ActivityId)==null)
                            {
                                db.ActivityShortDB.Add(ActivityShort);
                                db.SaveChanges();
                                Console.WriteLine($"Enterred Activities: {db.ActivityShortDB.OrderBy(b => b.ActivityId).Count()}");
                            }
                            else
                            {
                                Console.WriteLine($"{ActivityShort.ActivityId} allready in database");
                            }
                        }
                        arq.Status=QueryStatus.Done;
                        arq.StatusChanged=DateTime.Now;
                        db.SaveChanges();
                        ErrorCountConsecutive=0;
                    }
                    catch(Exception e)
                    {
                        db.SaveChanges();
                        ErrorCountConsecutive++;
                        ErrorCount++;
                        Console.WriteLine($"Error: {ErrorCountConsecutive}/3 total:{ErrorCount} -> skip:{arq} {e.Message}");
                        arq.Status=QueryStatus.Error;
                        arq.StatusChanged=DateTime.Now;
                        arq.Message=$"Error: {ErrorCountConsecutive}/3 total:{ErrorCount} -> skip:{arq} {e.Message}";
                        if (ErrorCountConsecutive>2)
                        {
                            // After 3 consecutive errors, I assume the selenium driver is down. Stop it all.
                            throw e;
                        }
                    }
                    Console.WriteLine($"Activities total stored = {db.ActivityShortDB.Count()}");
                    Console.WriteLine($"Request total stored = {db.ActivityQueriesDB.Count(a => a.Status==QueryStatus.Created)}");
                    Count++;
                    if (!KeepRunning || File.Exists("QueryActivities.quit"))
                    {
                        Console.WriteLine($"break {KeepRunning} {Count}");
                        break;
                    }
                }
            }
            return ret;
        }
    }
}

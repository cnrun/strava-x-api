using System;
using System.Threading;
using System.Linq;
using OpenQA.Selenium;
using Prototype.Model;
using System.Collections.Generic;

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
                foreach(ActivityRangeQuery arq in db.ActivityQueriesDB)
                {
                    try
                    {
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
                        db.Remove(arq);
                        db.SaveChanges();
                        ErrorCountConsecutive=0;
                    }
                    catch(Exception e)
                    {
                        ErrorCountConsecutive++;
                        ErrorCount++;
                        Console.WriteLine($"Error: {ErrorCountConsecutive}/3 total:{ErrorCount} -> skip:{arq} {e.Message}");
                        if (ErrorCountConsecutive>2)
                        {
                            // After 3 consecutive errors, I assume the selenium driver is down. Stop it all.
                            throw e;
                        }
                    }
                    Console.WriteLine($"Activities total stored = {db.ActivityShortDB.Count()}");
                    Console.WriteLine($"Request total stored = {db.ActivityQueriesDB.Count()}");
                    Count++;
                    if (!KeepRunning)
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

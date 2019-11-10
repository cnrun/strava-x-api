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
        static public void SendQueriesForActivities(string[] args)
        {
            Console.WriteLine("Create range queries.");
            using (StravaXApiContext db = new StravaXApiContext())
            {
                StravaXApi stravaXApi = StravaXApi.GetStravaXApi(args);
                stravaXApi.signIn();
                int Count=0;
                Boolean KeepRunning=true;
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
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine($"skip:{arq} {e.Message}");
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
        }
    }
}

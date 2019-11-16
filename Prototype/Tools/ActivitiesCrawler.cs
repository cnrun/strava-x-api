using System;
using System.Threading;
using System.Linq;
using OpenQA.Selenium;
using Prototype.Model;
using NDesk.Options;
using System.Collections.Generic;

namespace Prototype.Tools
{    
    public class ActivitiesCrawler
    {
        static internal void ReadActivitiesForAthlete(StravaXApi stravaXApi, string[] args)
        {
            Console.WriteLine("Read athlete activities with Strava-X-API.");

            String AthleteId = null;
            var p = new OptionSet () {
                { "a|athleteid=",   v => { AthleteId=v; } },
            };
            if (AthleteId==null)
            {
                p.WriteOptionDescriptions(Console.Out);
                throw new ArgumentException("missing athlete id");    
            }

            try
            {
                stravaXApi.signIn();
                List<ActivityShort> ActivitiesList = new List<ActivityShort>();

                DateTime FirstActivityDate = stravaXApi.getActivityRange(AthleteId);
                System.Console.WriteLine($"First activity at {FirstActivityDate.Year}/{FirstActivityDate.Month}");                    

                int FromYear=int.Parse(Environment.GetEnvironmentVariable("FROM_YEAR"));
                int FromMonth=int.Parse(Environment.GetEnvironmentVariable("FROM_MONTH"));
                int ToYear=int.Parse(Environment.GetEnvironmentVariable("TO_YEAR"));
                int ToMonth=int.Parse(Environment.GetEnvironmentVariable("TO_MONTH"));
                DateTime now = DateTime.Now;
                for(int year=FromYear;year<=ToYear;year++)
                {
                    for(int month=01;month<=12;month++)
                    {
                        if ((year<=FromYear && month<FromMonth) || (year>=ToYear && month>ToMonth))
                            continue;
                        List<ActivityShort> ActivitiesMonthList;
                        try
                        {
                            ActivitiesMonthList = stravaXApi.getActivities(AthleteId,$"{year:D4}",$"{month:D2}");
                        }
                        catch(StaleElementReferenceException)
                        {
                            // Wait and try again.
                            Thread.Sleep(2000);
                            ActivitiesMonthList = stravaXApi.getActivities(AthleteId,$"{year:D4}",$"{month:D2}");
                        }
                        ActivitiesList.AddRange(ActivitiesMonthList);
                        using (StravaXApiContext db = new StravaXApiContext())
                        {
                            foreach(ActivityShort ActivityShort in ActivitiesList)
                            {
                                Console.WriteLine($"JSON={ActivityShort.SerializePrettyPrint(ActivityShort)}");
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
                            Console.WriteLine($"total read = {ActivitiesList.Count}");
                            Console.WriteLine($"total stored = {db.ActivityShortDB.OrderBy(b => b.ActivityId).Count()}");
                            ActivitiesList.Clear();
                        }
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
}


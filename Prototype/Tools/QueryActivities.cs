using System;
using System.Linq;
using Prototype.Model;
using System.Collections.Generic;
using System.IO;
using Microsoft.EntityFrameworkCore;
using NDesk.Options;
using System.Diagnostics;

namespace Prototype.Tools
{    
    public class QueryActivities
    {
        static internal int SendQueriesForActivities(StravaXApi stravaXApi, string[] args)
        {
            int TimerSeconds = -1;
            int TimerExitCode = 2;
            var p = new OptionSet () {
                { "t|timer_sec=",   v => { TimerSeconds=int.Parse(v); } },
                { "e|timer_exit_code=",   v => { TimerExitCode=int.Parse(v); } },
            };
            p.Parse(args);

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
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                foreach(ActivityRangeQuery arq in queries)
                {
                    try
                    {
                        // https://docs.microsoft.com/en-us/ef/core/saving/concurrency
                        try
                        {
                            // reserve query, mark it as run.
                            arq.Status=QueryStatus.Run;
                            arq.StatusChanged=DateTime.Now;
                            db.SaveChanges();
                        }
                        catch(DbUpdateConcurrencyException)
                        {
                            // Just skip this entry if some conflict exists.
                            Console.WriteLine($"skip conflicted entry for {arq.AthleteId} at {arq.DateFrom.Year:D4}/{arq.DateFrom.Month:D2}");
                            continue;
                        }
                        var ActivitiesList = stravaXApi.getActivities(arq.AthleteId,$"{arq.DateFrom.Year:D4}",$"{arq.DateFrom.Month:D2}");
                        foreach(ActivityShort ActivityShort in ActivitiesList)
                        {
                            // Console.WriteLine($"JSON={ActivityShort.SerializePrettyPrint(ActivityShort)}");
                            if (db.ActivityShortDB.Find(ActivityShort.ActivityId)==null)
                            {
                                db.ActivityShortDB.Add(ActivityShort);
                                try{
                                    db.SaveChanges();
                                    Console.WriteLine($"Enterred Activities: {db.ActivityShortDB.OrderBy(b => b.ActivityId).Count()}");
                                }
                                catch(DbUpdateConcurrencyException)
                                {
                                    // Just skip this entry if some conflict exists.
                                    Console.WriteLine($"skip conflicted activity {ActivityShort}");
                                    continue;
                                }
                            }
                            else
                            {
                                Console.WriteLine($"{ActivityShort.ActivityId} allready in database");
                            }
                        }
                        arq.Status=QueryStatus.Done;
                        arq.StatusChanged=DateTime.Now;
                        // should not have to save anything.
                        db.SaveChanges();
                        ErrorCountConsecutive=0;
                    }
                    // catch(DbUpdateConcurrencyException)
                    // {
                    // }
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
                    TimeSpan ts = stopWatch.Elapsed;
                    if (TimerSeconds>0 && ts.TotalSeconds>TimerSeconds)
                    {
                        Console.WriteLine($"Timer reached after {ts.ToString()} now exit with {TimerExitCode}.");
                        ret = TimerExitCode;
                        break;
                    }
                    // Exist when KeepRunning is false (from the debugger),
                    // or the file 'QueryActivities.quit' exists.
                    //   *Program will exit with "touch QueryActivities.quit" in /app directory in container.
                    if (!KeepRunning || File.Exists("QueryActivities.quit"))
                    {
                        Console.WriteLine($"break {KeepRunning} {Count}");
                        break;
                    }
                }
                ret = 0;
            }
            return ret;
        }
    }
}

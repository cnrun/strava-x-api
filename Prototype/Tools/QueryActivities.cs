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
            string clientId = new Guid().ToString();
            int TimerSeconds = -1;
            int TimerExitCode = 2;
            var p = new OptionSet () {
                { "t|timer_sec=",   v => { TimerSeconds=int.Parse(v); } },
                { "e|timer_exit_code=",   v => { TimerExitCode=int.Parse(v); } },
            };
            p.Parse(args);

            int ret = -1;
            Console.WriteLine($"Query activities. Client:{clientId}");
            using (StravaXApiContext db = new StravaXApiContext())
            {
                stravaXApi.signIn();
                int Count=0;
                Boolean KeepRunning=true;
                int ErrorCountConsecutive=0;
                int ErrorCount=0;

                // 1) find an athlete with opened queries and without reserved queries.
                // string aid =db.ActivityQueriesDB.Where(a => a.Status==QueryStatus.Created).First().AthleteId;
                // select distinct AthleteId from [dbo].[ActivityQueriesDB] where Status=2
                // intersect
                // select distinct AthleteId from [dbo].[ActivityQueriesDB] where (Status<>0 AND Status<>3)

                var q1 = db.ActivityQueriesDB.Where(a => a.Status==QueryStatus.Created).Select(a => a.AthleteId).Distinct();    
                var q2 = db.ActivityQueriesDB.Where(a => a.Status!>QueryStatus.Reserved).Select(a => a.AthleteId).Distinct();    
                List<string> AthleteIdList = q1.Intersect(q2).Take(100).ToList();
                string aid =AthleteIdList.ElementAt(new Random().Next(AthleteIdList.Count));
                Console.WriteLine($"retrieve activity for athlete {aid}");

                // https://docs.microsoft.com/en-us/ef/ef6/querying/
                // First retrieve all query objects to avoid "New transaction is not allowed because there are other threads running in the session."
                // Not best praxis but enought for Prototype.
                // https://stackoverflow.com/a/2656612/281188
                // IList<ActivityRangeQuery> queries = db.ActivityQueriesDB.Where(a => a.Status==QueryStatus.Created).OrderByDescending(a => a.DateFrom).Take(50).ToList();
                IList<ActivityRangeQuery> q0 = db.ActivityQueriesDB.Where(a => a.AthleteId==aid && a.Status==QueryStatus.Created).OrderByDescending(a => a.DateFrom).Take(50).ToList();
                if (q0.Count==0)
                {
                    // no queries anymore for this athlete, choose another one.
                    // should not happens because we have explicitly searched after athlete with query status "CREATED"
                    Console.WriteLine($"WARNING: some valide queries have been found for {aid} not entry has been found.");
                }

                // Mark all queries with "RESERVED"
                IList<ActivityRangeQuery> queries = new List<ActivityRangeQuery>();
                foreach(ActivityRangeQuery arq in q0)
                {
                    try
                    {
                        arq.Status=QueryStatus.Reserved;                    
                        arq.StatusChanged=DateTime.Now;
                        arq.Message=$"Reserved by {clientId}";
                        db.SaveChanges();
                        queries.Add(arq);
                    }
                    catch(DbUpdateConcurrencyException)
                    {
                        // Just skip this entry if some conflict exists.
                        Console.WriteLine($"skip: Can't reserve query {arq.AthleteId} at {arq.DateFrom.Year:D4}/{arq.DateFrom.Month:D2} for {clientId}");
                        continue;
                    }
                }
                // Run for all reserved queries
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                ret = 0;
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
                    catch(Exception e)
                    {
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
                        // exit with error code, container should restart
                        ret = TimerExitCode;
                        break;
                    }
                    // Exist when KeepRunning is false (from the debugger),
                    // or the file 'QueryActivities.quit' exists.
                    //   *Program will exit with "touch QueryActivities.quit" in /app directory in container.
                    if (!KeepRunning || File.Exists("QueryActivities.quit"))
                    {
                        Console.WriteLine($"break {KeepRunning} {Count}");
                        // regular exit, container should ended.
                        ret = 0;
                        break;
                    }
                }
            }
            return ret;
        }
    }
}

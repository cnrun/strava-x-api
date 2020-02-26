using System;
using System.Linq;
using Strava.XApi.Model;
using System.Collections.Generic;
using System.IO;
using Microsoft.EntityFrameworkCore;
using NDesk.Options;
using System.Diagnostics;

namespace Strava.XApi.Tools
{
    class CancelExecution:Exception
    {
        public CancelExecution(string msg):base(msg) {}
    }
    public class QueryActivities
    {
        static private int Count=0;
        static private int ErrorCountConsecutive=0;
        static private int ErrorCount=0;
        static internal int SendQueriesForActivities(StravaXApi stravaXApi, string[] args)
        {
            string clientId = Guid.NewGuid().ToString();
            int TimerSeconds = -1;
            int TimerExitCode = 2;
            bool doVerbose=false;
            var p = new OptionSet () {
                { "t|timer_sec=",   v => { TimerSeconds=int.Parse(v); } },
                { "e|timer_exit_code=",   v => { TimerExitCode=int.Parse(v); } },
                { "v|verbose",   v => { doVerbose=true; } },
            };
            p.Parse(args);

            int ret = -1;
            Console.WriteLine($"Query activities. Client:{clientId}");
            using (StravaXApiContext db = new StravaXApiContext())
            {
                stravaXApi.signIn();

                // 1) find an athlete with opened queries and without reserved queries.
                // string aid =db.ActivityQueriesDB.Where(a => a.Status==QueryStatus.Created).First().AthleteId;
                // select distinct AthleteId from [dbo].[ActivityQueriesDB] where Status=2
                // intersect
                // select distinct AthleteId from [dbo].[ActivityQueriesDB] where (Status<>0 AND Status<>3)
                var qAthleteCreated = db.ActivityQueriesDB.Where(a => a.Status==QueryStatus.Created).Select(a => a.AthleteId).Distinct();
                var qAthleteReserved = db.ActivityQueriesDB.Where(a => a.Status!=QueryStatus.Reserved).Select(a => a.AthleteId).Distinct();
                List<string> AthleteIdList = qAthleteCreated.Intersect(qAthleteReserved).Take(100).ToList();

                if (AthleteIdList.Count==0)
                {
                    Console.WriteLine($"no more athlete to search for. created:{qAthleteCreated.Count()} reserved:{qAthleteReserved.Count()}");
                    // TODO some queries may have been reserved, but no Thread is working on it. It should be usefull to detect and reset.
                }
                else
                {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();
                    ret=0;
                    try {
                        while(AthleteIdList.Count>0 && ret==0)
                        {
                            string aid =AthleteIdList.ElementAt(new Random().Next(AthleteIdList.Count));
                            Console.WriteLine($"retrieve activity for athlete {aid}");

                            // https://docs.microsoft.com/en-us/ef/ef6/querying/
                            // First retrieve all query objects to avoid "New transaction is not allowed because there are other threads running in the session."
                            // Not best praxis but enought for Prototype.
                            // https://stackoverflow.com/a/2656612/281188
                            // IList<ActivityRangeQuery> queries = db.ActivityQueriesDB.Where(a => a.Status==QueryStatus.Created).OrderByDescending(a => a.DateFrom).Take(50).ToList();
                            ret = queryAthlete(aid, clientId, stravaXApi, db, doVerbose);

                            TimeSpan ts = stopWatch.Elapsed;
                            if (TimerSeconds>0 && ts.TotalSeconds>TimerSeconds)
                            {
                                ret = TimerExitCode;
                                Console.WriteLine($"Timer reached after {ts.ToString()} now exit with {ret}.");
                                // exit with error code, container should restart
                                break;
                            }
                            Boolean KeepRunning=true;
                            if (!KeepRunning || File.Exists("QueryActivities.quit"))
                            {
                                Console.WriteLine($"break {KeepRunning} {Count}");
                                // regular exit, container should ended.
                                ret = 0;
                                break;
                            }
                            // search for a new athlete.
                            qAthleteCreated = db.ActivityQueriesDB.Where(a => a.Status==QueryStatus.Created).Select(a => a.AthleteId).Distinct();
                            qAthleteReserved = db.ActivityQueriesDB.Where(a => a.Status!=QueryStatus.Reserved).Select(a => a.AthleteId).Distinct();
                            AthleteIdList = qAthleteCreated.Intersect(qAthleteReserved).Take(100).ToList();
                        }
                    }
                    catch(CancelExecution e)
                    {
                        Console.WriteLine($"Execution has been canceled: {e.Message}");
                    }
                }
            }
            return ret;
        }
        static int queryRange(StravaXApi stravaXApi, StravaXApiContext db, IList<ActivityRangeQuery> queries, bool doVerbose)
        {
            int ret = 0;
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
                    int EnterredActivityCount=0;
                    foreach(ActivityShort ActivityShort in ActivitiesList)
                    {
                        // Console.WriteLine($"JSON={ActivityShort.SerializePrettyPrint(ActivityShort)}");
                        if (db.ActivityShortDB.Find(ActivityShort.ActivityId)==null)
                        {
                            db.ActivityShortDB.Add(ActivityShort);
                            try{
                                // do it better after foreach, but first check how it works with the concurrentcy exception
                                db.SaveChanges();
                                EnterredActivityCount++;
                            }
                            catch(DbUpdateConcurrencyException)
                            {
                                // Just skip this entry if some conflict exists.
                                Console.WriteLine($"❌ skip conflicted activity {ActivityShort}");
                                continue;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"❌ {ActivityShort.ActivityId} allready in database");
                        }
                    }
                    arq.Status=QueryStatus.Done;
                    arq.StatusChanged=DateTime.Now;
                    // should not have to save anything.
                    db.SaveChanges();
                    if (doVerbose)
                        Console.WriteLine($"enterred activity count: {EnterredActivityCount}/{db.ActivityShortDB.Count()} for {arq.AthleteId} at {arq.DateFrom.Year:D4}/{arq.DateFrom.Month:D2}");
                    else // '.Count()' is pretty expensive, skip it if verbose should be minimal.
                        Console.WriteLine($"enterred activity count: {EnterredActivityCount} for {arq.AthleteId} at {arq.DateFrom.Year:D4}/{arq.DateFrom.Month:D2}");
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

                if (doVerbose)
                    Console.WriteLine($"activities stored:{db.ActivityShortDB.Count()}/{QueryStatus.Created}:{db.ActivityQueriesDB.Count(a => a.Status==QueryStatus.Created)}/{QueryStatus.Reserved}:{db.ActivityQueriesDB.Count(a => a.Status==QueryStatus.Reserved)}");

                Count++;

                // Exist when KeepRunning is false (from the debugger),
                // or the file 'QueryActivities.quit' exists.
                //   *Program will exit with "touch QueryActivities.quit" in /app directory in container.
                Boolean KeepRunning=true;
                if (!KeepRunning || File.Exists("QueryActivities.quit.immediatly"))
                {
                    Console.WriteLine($"break {KeepRunning} {Count}");
                    // regular exit, container should ended.
                    throw new CancelExecution("break {KeepRunning} {Count}");
                }
            }
            return ret;
        }

        static int queryAthlete(string aid, string clientId, StravaXApi stravaXApi, StravaXApiContext db, bool doVerbose)
        {
            int ret = 0;
            IList<ActivityRangeQuery> q0 = db.ActivityQueriesDB.Where(a => a.AthleteId==aid && a.Status==QueryStatus.Created).OrderByDescending(a => a.DateFrom).Take(12).ToList();
            while(q0.Count>0 && ret==0)
            {
                // Mark all queries with "RESERVED"
                IList<ActivityRangeQuery> queries = new List<ActivityRangeQuery>();
                foreach(ActivityRangeQuery arq in q0)
                {
                    try
                    {
                        arq.Status=QueryStatus.Reserved;
                        arq.StatusChanged=DateTime.Now;
                        arq.Message=$"Reserved by {clientId}";
                        queries.Add(arq);
                    }
                    catch(DbUpdateConcurrencyException)
                    {
                        // Just skip this entry if some conflict exists.
                        Console.WriteLine($"skip: Can't reserve query {arq.AthleteId} at {arq.DateFrom.Year:D4}/{arq.DateFrom.Month:D2} for {clientId}");
                        continue;
                    }
                }
                db.SaveChanges();
                // Run for all reserved queries
                try
                {
                    ret = queryRange(stravaXApi, db, queries, doVerbose);
                }
                catch(CancelExecution e)
                {
                    // just to clarify that it may happend.
                    // finally will also call in that case.
                    throw e;
                }
                finally
                {
                    // integrity check, status should not be Reserved anymore
                    foreach(ActivityRangeQuery arq in q0)
                    {
                        if (arq.Status==QueryStatus.Reserved)
                        {
                            Console.WriteLine($"WARN: remove reservation on {arq}");
                        }
                    }
                    db.SaveChanges();
                }
                // retrieve next queries for max one year.
                q0 = db.ActivityQueriesDB.Where(a => a.AthleteId==aid && a.Status==QueryStatus.Created).OrderByDescending(a => a.DateFrom).Take(12).ToList();
            }
            return ret;
        }
    }
}

using System;
using System.Linq;
using Prototype.Model;
using System.Collections.Generic;
using NDesk.Options;
using Microsoft.Extensions.Logging;

namespace Prototype.Tools
{    
    public class DbStats
    {
        static public int WriteState(string[] args)
        {
            
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", Microsoft.Extensions.Logging.LogLevel.Warning)
                    .AddFilter("System", Microsoft.Extensions.Logging.LogLevel.Warning)
                    .AddFilter("Prototype.Tools.DbStats", Microsoft.Extensions.Logging.LogLevel.Debug)
                    .AddFilter("StravaXApi", Microsoft.Extensions.Logging.LogLevel.Information)
                    .AddProvider(new CustomLoggerProvider());
                    //.AddEventLog();
            });
            var logger = loggerFactory.CreateLogger<StravaXApi>();
            logger.LogDebug("Log Stats");

            bool doGarbage=false;
            bool doAll=false;
            bool doListimages=false;
            string AthleteId=null;
            bool doAthleteStats=false;
            var p = new OptionSet () {
                { "g|garbage",   v => { doGarbage=true; } },
                { "a|all",   v => { doAll=true; } },
                { "l|listimages",   v => { doListimages=true; } },
                { "aid|athleteid=",   v => { AthleteId=v; } },
                { "as|athlete-stats",   v => { doAthleteStats=true; } },
            };
            p.Parse(args);

            int ret = -1;
            try
            {
                using (StravaXApiContext db = new StravaXApiContext())
                {
                    logger.LogInformation($"Queries stored {db.ActivityQueriesDB.Count()}");
                    logger.LogInformation($"Activities stored {db.ActivityShortDB.Count()}");
                    var al = db.ActivityShortDB.Select(a => a.AthleteId).Distinct();
                    logger.LogInformation($"Athletes {al.Count()} from {db.AthleteShortDB.Count()}");
                    /*
                    foreach(var aId in al)
                    {
                        AthleteShort ath = db.AthleteShortDB.Find(aId);
                        // for format: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/interpolated
                        if (ath != null)
                        {                            
                            logger.LogInformation($" Activities:{db.ActivityShortDB.Count(a => a.AthleteId==aId),6} for {ath.AthleteId,9} {ath.AthleteName,-40} {ath.AthleteLocation}");
                        }
                        else
                        {
                            logger.LogInformation($" Activities:{db.ActivityShortDB.Count(a => a.AthleteId==aId),6} for {aId,9}");
                        }
                    }
                    */
                    /*
                    // output for first query
                    foreach(ActivityRangeQuery arq in db.ActivityQueriesDB)
                    {
                        logger.LogInformation($"{arq}");
                        break;
                    }
                    */


                    if (doAll)
                    {
                        // output status count for queries
                        var status = db.ActivityQueriesDB.Select(a => a.Status).Distinct();
                        logger.LogInformation($"Queries:");
                        foreach(var st in status)
                        {
                            logger.LogInformation($" {st} {db.ActivityQueriesDB.Count(a => a.Status==st)}");
                        }
                    }

                    if (doGarbage)
                    {
                        {
                            var statusError = db.ActivityQueriesDB.Where(a => a.Status==QueryStatus.Error);
                            foreach(ActivityRangeQuery arq in statusError.ToList())
                            {
                                logger.LogInformation($"query with {QueryStatus.Error} {arq.AthleteId} {arq.Message}");
                            }
                        }
                        {
                            var statusRun = db.ActivityQueriesDB.Where(a => a.Status==QueryStatus.Run);
                            foreach(ActivityRangeQuery arq in statusRun.ToList())
                            {
                                arq.Status=QueryStatus.Created;
                                arq.StatusChanged=DateTime.Now;
                                arq.Message=$"garbage-reset from {QueryStatus.Run} to {QueryStatus.Created}";
                                logger.LogInformation($"Reset {arq.AthleteId} {arq.DateFrom} {arq.Message}");
                            }
                            db.SaveChanges();
                        }
                        {
                            var statusReserved = db.ActivityQueriesDB.Where(a => a.Status==QueryStatus.Reserved);
                            foreach(ActivityRangeQuery arq in statusReserved.ToList())
                            {
                                arq.Status=QueryStatus.Created;
                                arq.StatusChanged=DateTime.Now;
                                arq.Message=$"garbage-reset from {QueryStatus.Reserved} to {QueryStatus.Created}";
                                logger.LogInformation($"Reset {arq.AthleteId} {arq.DateFrom} {arq.Message}");
                                db.SaveChanges();
                            }
                        }
                    }

                    if (doAll)
                    {
                        // Find out athlete with open queries:
                        var qAthleteCreated = db.ActivityQueriesDB.Where(a => a.Status==QueryStatus.Created).Select(a => a.AthleteId).Distinct();    
                        logger.LogInformation($"{qAthleteCreated.Count()} with {QueryStatus.Created} queries");
                        var qAthleteReserved = db.ActivityQueriesDB.Where(a => a.Status!=QueryStatus.Reserved).Select(a => a.AthleteId).Distinct();    
                        logger.LogInformation($"{qAthleteCreated.Count()} without {QueryStatus.Reserved} queries");
                        logger.LogInformation($"{qAthleteCreated.Intersect(qAthleteReserved).Count()} with {QueryStatus.Created} and without {QueryStatus.Reserved} queries");
                        List<string> AthleteIdList = qAthleteCreated.Intersect(qAthleteReserved).Take(10).ToList();
                        logger.LogInformation($"{AthleteIdList.Count()} athletes from this list:");
                        foreach(string aId in AthleteIdList)
                        {
                            logger.LogInformation($" AthleteId={aId}");
                        }
                        if (AthleteIdList.Count()>0)
                        {
                            // retrieve one random athlete
                            string aid =AthleteIdList.ElementAt(new Random().Next(AthleteIdList.Count));
                            logger.LogInformation($"retrieve activity for athlete {aid}");
                            // 5 first queries for this athlete
                            IList<ActivityRangeQuery> q0 = db.ActivityQueriesDB.Where(a => a.AthleteId==aid && a.Status==QueryStatus.Created).OrderByDescending(a => a.DateFrom).Take(10).ToList();
                            foreach(ActivityRangeQuery arq in q0)
                            {
                                logger.LogInformation($" query={arq}");
                                var ActivitiesInRange = db.ActivityShortDB.Where(a=>a.AthleteId==arq.AthleteId && ((a.ActivityDate>=arq.DateFrom)&&(a.ActivityDate<=arq.DateTo)));
                                logger.LogInformation($"     find {ActivitiesInRange.Count()} activities in it.");
                            }
                        }
                    }

                    if (doAll)
                    {
                        // find activities in a QueryRange
                        foreach(ActivityRangeQuery arq in db.ActivityQueriesDB.Where(a => a.Status==QueryStatus.Done).OrderByDescending(a=>a.DateFrom).Take(10))
                        {
                            var ActivitiesInRange = db.ActivityShortDB.Where(a=>a.AthleteId==arq.AthleteId && ((a.ActivityDate>=arq.DateFrom)&&(a.ActivityDate<=arq.DateTo)));
                            logger.LogInformation($" find {ActivitiesInRange.Count()} in range {arq}");
                        }
                    }

                    if (doAll)
                    {
                        // retrieve queries type for athlete
                        string aId="2754335";
                        var qsList = db.ActivityQueriesDB.Where(a => a.AthleteId==aId).Select(aId=>aId.Status).Distinct().ToList();
                        logger.LogInformation($" athl {aId}");
                        foreach(QueryStatus qs in qsList)
                        {
                            logger.LogInformation($" query {qs} count {db.ActivityQueriesDB.Where(a => a.AthleteId==aId && a.Status==qs).Count()}");
                        }
                        // IList<ActivityRangeQuery> q0 = db.ActivityQueriesDB.Where(a => a.AthleteId==aId && a.Status==QueryStatus.Created).OrderByDescending(a => a.DateFrom).Take(6).ToList();
                    }
                    if (doAll)
                    {
                        string aId="26319532";
                        var qsList = db.ActivityQueriesDB.Where(a => a.AthleteId==aId).Select(aId=>aId.Status).Distinct().ToList();
                        logger.LogInformation($" athl {aId}");
                        foreach(QueryStatus qs in qsList)
                        {
                            logger.LogInformation($" query {qs} count {db.ActivityQueriesDB.Where(a => a.AthleteId==aId && a.Status==qs).Count()}");
                        }
                    }
                    if (doAll || doListimages)
                    {
                        List<ActivityShort> activitiesWithImages = db.ActivityShortDB.Where(a => a.ActivityImagesListAsString.Length>0 && a.ActivityType==ActivityType.BackcountrySki).ToList();
                        logger.LogInformation($" activity count:{activitiesWithImages.Count()}");
                        int imageCount=0;
                        foreach(ActivityShort activity in activitiesWithImages)
                        {
                            List<string> images = activity.ActivityImagesList;
                            imageCount+=images.Count();
                            logger.LogInformation($"activity {activity.ActivityTitle}");
                            foreach(string imageUrl in images)
                            {
                               logger.LogInformation($" url:{imageUrl}");
                            }
                        }
                        logger.LogInformation($" images {imageCount}");
                    }

                    if (doAll || doAthleteStats)
                    {
                        IList<AthleteShort> AllAthletes = db.AthleteShortDB.ToList();
                        logger.LogInformation($" athletes {AllAthletes.Count()}");
                        logger.LogInformation($" first: [{AllAthletes.First()}] last: [{AllAthletes.Last()}]");
                        int AthleteCount = db.ActivityQueriesDB.Select(a=>a.AthleteId).Distinct().Count();
                        logger.LogInformation($"athletes in queries {AthleteCount}");
                        int PrivateAthleteCount = db.ActivityQueriesDB.Where(a => a.Status==QueryStatus.Done && a.Message.Contains("private")).Select(a=>a.AthleteId).Distinct().Count();
                        logger.LogInformation($"  private {PrivateAthleteCount}");
                        int CreatedAthleteCount = db.ActivityQueriesDB.Where(a => a.Status==QueryStatus.Created).Select(a=>a.AthleteId).Distinct().Count();
                        logger.LogInformation($"  {QueryStatus.Created} {CreatedAthleteCount}");
                    }
                    /*
                    {
                        // opened queries with activities
                        int i=0;
                        int nbAthletes=db.AthleteShortDB.Count();
                        foreach(AthleteShort athlete in db.AthleteShortDB)
                        {
                            i++;
                            // logger.LogInformation($"retrieve activity for athlete {athlete.AthleteId}");
                            // 5 first queries for this athlete
                            IList<ActivityRangeQuery> q0 = db.ActivityQueriesDB.Where(a => a.AthleteId==athlete.AthleteId && a.Status==QueryStatus.Created).OrderByDescending(a => a.DateFrom).Take(5).ToList();
                            int activitiesWithCreatedQueriesCount=0;
                            foreach(ActivityRangeQuery arq in q0)
                            {
                                var ActivitiesInRange = db.ActivityShortDB.Where(a=>a.AthleteId==arq.AthleteId && ((a.ActivityDate>=arq.DateFrom)&&(a.ActivityDate<=arq.DateTo)));
                                activitiesWithCreatedQueriesCount+=ActivitiesInRange.Count();
                            }
                            if (activitiesWithCreatedQueriesCount>0)
                            {
                                logger.LogInformation($" athlete {athlete.AthleteId} as {activitiesWithCreatedQueriesCount} activities with created queries queries:{q0.Count()} activites{db.ActivityShortDB.Where(a=>a.AthleteId==athlete.AthleteId).Count()}.");
                            }
                            if (i%10==0)
                            {
                                logger.LogInformation($"{i}/{nbAthletes}");
                            }
                        }
                    }
                    */
                    /*
                    // output activity count for activity type
                    logger.LogInformation($"Activity types Σ :{db.ActivityShortDB.Count()}");
                    var ActivityTypes = db.ActivityShortDB.Select(a => a.ActivityType).Distinct();
                    foreach(var aType in ActivityTypes)
                    {
                        logger.LogInformation($" {aType,18} {db.ActivityShortDB.Count(a => a.ActivityType==aType),6}");
                    }
                    */

                    /*
                    // output athletes with ski activities and their ski activity count
                    logger.LogInformation($"All athletes with {ActivityType.BackcountrySki}");
                    var Activity4Type = db.ActivityShortDB.Where(a => a.ActivityType==ActivityType.BackcountrySki).Select(a => a.AthleteId).Distinct();
                    foreach(var A4Type in Activity4Type)
                    {
                        var count = db.ActivityShortDB.Where(a => a.ActivityType==ActivityType.BackcountrySki).Where(a => a.AthleteId==A4Type).Count();
                        var athlete = db.AthleteShortDB.Find(A4Type);
                        logger.LogInformation($" {athlete?.AthleteName,30} : {A4Type,8} ({count})");
                    }
                    */

                    /*
                    // output athletes with run activities and their run activity count
                    logger.LogInformation($"All athletes with {ActivityType.Run}");
                    Activity4Type = db.ActivityShortDB.Where(a => a.ActivityType==ActivityType.Run).Select(a => a.AthleteId).Distinct();
                    foreach(var A4Type in Activity4Type)
                    {
                        var count = db.ActivityShortDB.Where(a => a.ActivityType==ActivityType.Run).Where(a => a.AthleteId==A4Type).Count();
                        var athlete = db.AthleteShortDB.Find(A4Type);
                        logger.LogInformation($" {athlete?.AthleteName,30} : {A4Type,8} ({count})");
                    }
                    */
                }
                ret = 0 ;
            }
            catch(Exception e)
            {
                logger.LogInformation($"ERROR:{e.ToString()}"); 
                ret = 1; 
            }
            return ret;
        }
    }
}

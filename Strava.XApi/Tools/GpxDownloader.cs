using System;
using NDesk.Options;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.IO;
using Strava.XApi.Model;
using System.Collections.Generic;

namespace Strava.XApi.Tools
{    
    public class GpxDownloader
    {
        static internal int Downloader(StravaXApi stravaXApi, string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", Microsoft.Extensions.Logging.LogLevel.Warning)
                    .AddFilter("System", Microsoft.Extensions.Logging.LogLevel.Warning)
                    .AddFilter("Strava.XApi.Tools.GpxDownloader", Microsoft.Extensions.Logging.LogLevel.Debug)
                    .AddFilter("StravaXApi", Microsoft.Extensions.Logging.LogLevel.Information)
                    .AddProvider(new CustomLoggerProvider());
                    //.AddEventLog();
            });
            var logger = loggerFactory.CreateLogger<StravaXApi>();
            logger.LogDebug("Log ImageDownloader");
            bool doGpx=false;
            string maxCountStr = null;
            string AthleteId = null;
            string ActivityTypeStr = null;
            var p = new OptionSet () {
                { "g|gpx",   v => { doGpx=true; } },
                { "a|athleteid=",   v => { AthleteId=v; } },
                { "at|activity_type=",   v => { ActivityTypeStr=v; } },
                { "m|max_count=",   v => { maxCountStr=v; } },
            };
            p.Parse(args);
            int ret = -1;

            try
            {
                List<ActivityShort> activities;
                using (StravaXApiContext db = new StravaXApiContext())
                {
                    IQueryable<ActivityShort> dbs = db.ActivityShortDB.OrderByDescending(b => b.ActivityDate);
                    if (ActivityTypeStr!=null)
                    {
                        ActivityType ActivityType=(ActivityType)Enum.Parse(typeof(ActivityType),ActivityTypeStr);
                        dbs=dbs.Where(a => a.ActivityType==ActivityType);
                    }
                    if (AthleteId!=null)
                    {
                        dbs=dbs.Where(a => a.AthleteId==AthleteId);
                    }
                    // Ignore Activities without image map. They have probably been enterred without gps-track.
                    dbs=dbs.Where(a => a.ActivityImageMapUrl!=null);
                    activities = dbs.ToList();
                }
                logger.LogInformation($"BEGIN GPX Download for {(AthleteId==null?"all athletes":AthleteId)}/{(ActivityTypeStr==null?"all types":ActivityTypeStr)} :{activities.Count()}");
                int countSkipped=0;
                int countDownload=0;
                if (doGpx && activities.Count>0)
                {
                    bool needTracksDownload=false;
                    foreach(ActivityShort activity in activities)
                    {
                        string outputDir=$"gpx/{activity.AthleteId}";
                        string outputFilename=$"{activity.ActivityId}_{activity.AthleteId}.gpx";
                        if (!File.Exists($"{outputDir}/{outputFilename}"))
                        {
                            needTracksDownload=true;
                            break;
                        }
                    }
                    if (needTracksDownload)
                    {
                        stravaXApi.signIn();
                        foreach(ActivityShort activity in activities)
                        {
                            string outputDir=$"gpx/{activity.AthleteId}";
                            // fi.MoveTo($"{fi.Directory.FullName}/{ActivityId}_{fi.Name}");
                            string outputFilename=$"{activity.ActivityId}_{activity.AthleteId}.gpx";
                            if (!File.Exists($"{outputDir}/{outputFilename}"))
                            {
                                if (!Directory.Exists(outputDir))
                                {
                                    DirectoryInfo DirInfo = Directory.CreateDirectory(outputDir);
                                    logger.LogDebug($"directory for GPX created at {DirInfo.FullName}");
                                }
                                try
                                {
                                    stravaXApi.getActivityGpxSelenium(activity.ActivityId,$"{outputDir}/{outputFilename}");
                                    countDownload++;
                                }
                                catch(PrivateGpxException e)
                                {
                                    logger.LogDebug($"GPX Track private for {activity.AthleteId}: {e.ActivityId} {e.Message}");
                                    // Write error file
                                    using (StreamWriter outputFile = new StreamWriter(Path.Combine(outputDir, outputFilename)))
                                    {
                                        outputFile.WriteLine($"Error while downloading GPX for athlete:{activity.AthleteId} activity{activity.ActivityId}");
                                        outputFile.WriteLine($"{e.Message}");
                                        outputFile.WriteLine($"{e.StackTrace}");
                                    }
                                    countSkipped++;
                                }
                            }
                            else
                            {
                                logger.LogDebug($"GPX Track already downloaded for {activity.AthleteId}");
                                countSkipped++;
                            }
                        }
                    }
                }
                logger.LogInformation($"DONE GPX Download {countDownload} (skipped: {countSkipped})for {(AthleteId==null?"all athletes":AthleteId)}/{(ActivityTypeStr==null?"all types":ActivityTypeStr)} :{activities.Count()}");
            }
            catch(Exception e)
            {
                logger.LogError($"ERROR:{e.ToString()}");  
                ret = 1;
            }
            finally
            {
                stravaXApi.Dispose();
            }

            return ret;
        }
    }
}
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
            string gpxDir = "gpx";
            var p = new OptionSet () {
                { "g|gpx",   v => { doGpx=true; } },
                { "a|athleteid=",   v => { AthleteId=v; } },
                { "at|activity_type=",   v => { ActivityTypeStr=v; } },
                { "m|max_count=",   v => { maxCountStr=v; } },
                { "d|dirname=",   v => { gpxDir=v; } },
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
                    // retrieve the activities from the database.
                    activities = dbs.ToList();
                }
                logger.LogInformation($"BEGIN GPX Download for {(AthleteId==null?"all athletes":AthleteId)}/{(ActivityTypeStr==null?"all types":ActivityTypeStr)} :{activities.Count()}");
                int countSkipped=0;
                int countDownload=0;
                if (doGpx && activities.Count>0)
                {
                    bool needTracksDownload=false;
                    // detect if any activity needs a gpx file
                    foreach(ActivityShort activity in activities)
                    {
                        string outputDir=$"{gpxDir}/{activity.AthleteId}";
                        string outputFilename=$"{activity.ActivityId}_{activity.AthleteId}.gpx";
                        if (!File.Exists($"{outputDir}/{outputFilename}"))
                        {
                            needTracksDownload=true;
                            break;
                        }
                    }
                    // if no track is needed, we could exit without signin in Strava.
                    if (needTracksDownload)
                    {
                        // signin
                        stravaXApi.signIn();
                        // go throw all activities.
                        foreach(ActivityShort activity in activities)
                        {
                            string outputDir=$"{gpxDir}/{activity.AthleteId}";
                            // fi.MoveTo($"{fi.Directory.FullName}/{ActivityId}_{fi.Name}");
                            string outputFilename=$"{activity.ActivityId}_{activity.AthleteId}.gpx";
                            string outputFilenameGZip=$"{outputFilename}.gz";
                            string outputFilenameErr=$"{outputFilename}.err";
                            // download only if the gpx, gz or the err file do not exists.
                            if (!File.Exists($"{outputDir}/{outputFilename}") && !File.Exists($"{outputDir}/{outputFilenameGZip}") && !File.Exists($"{outputDir}/{outputFilenameErr}"))
                            {
                                // create directory if needed
                                if (!Directory.Exists(outputDir))
                                {
                                    DirectoryInfo DirInfo = Directory.CreateDirectory(outputDir);
                                    logger.LogDebug($"directory for GPX created at {DirInfo.FullName}");
                                }
                                try
                                {
                                    stravaXApi.getActivityGpxSelenium(activity.ActivityId,$"{outputDir}/{outputFilenameGZip}");
                                    countDownload++;
                                }
                                catch(PrivateGpxException e)
                                {
                                    logger.LogDebug($"GPX Track private for {activity.AthleteId}: {e.ActivityId} {e.Message}");
                                    // write error file, do prevent a second try.
                                    using (StreamWriter outputFile = new StreamWriter(Path.Combine(outputDir, outputFilenameErr)))
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
                else if (! doGpx)
                {
                    // just retrieve without retrieving the gpx files.
                    int countToDownload=0;
                    int countDownloaded=0;
                    foreach(ActivityShort activity in activities)
                    {
                        // logger.LogInformation($"activity {activity.StatShortString} -> {Utils.extractActivityTime(activity)}.");
                        string outputDir=$"{gpxDir}/{activity.AthleteId}";
                        // fi.MoveTo($"{fi.Directory.FullName}/{ActivityId}_{fi.Name}");
                        string outputFilename=$"{activity.ActivityId}_{activity.AthleteId}.gpx";
                        if (!File.Exists($"{outputDir}/{outputFilename}"))
                        {
                            countToDownload++;
                        }
                        else
                        {
                            countDownloaded++;
                        }
                    }
                    logger.LogInformation($"GPX Track to download:{countToDownload} already downloaded:{countDownloaded}");
                }
                logger.LogInformation($"DONE GPX Download {countDownload} (skipped: {countSkipped})for {(AthleteId==null?"all athletes":AthleteId)}/{(ActivityTypeStr==null?"all types":ActivityTypeStr)} :{activities.Count()}");
                // everything done, with return 0 we are telling the container, that there's no need to restart.
                ret = 0;
            }
            catch(Exception e)
            {
                logger.LogError($"ERROR:{e.ToString()}");
                // return error code, container should restart.
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
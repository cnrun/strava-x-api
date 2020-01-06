using System;
using System.Linq;
using System.Net;
using System.IO;
using Prototype.Model;
using System.Collections.Generic;
using NDesk.Options;
using Microsoft.Extensions.Logging;

namespace Prototype.Tools
{    
    public class ImageDownloader
    {
        static public int Downloader(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", Microsoft.Extensions.Logging.LogLevel.Warning)
                    .AddFilter("System", Microsoft.Extensions.Logging.LogLevel.Warning)
                    .AddFilter("Prototype.Tools.ImageDownloader", Microsoft.Extensions.Logging.LogLevel.Debug)
                    .AddFilter("StravaXApi", Microsoft.Extensions.Logging.LogLevel.Information)
                    .AddProvider(new CustomLoggerProvider());
                    //.AddEventLog();
            });
            var logger = loggerFactory.CreateLogger<StravaXApi>();
            logger.LogDebug("Log ImageDownloader");
            bool doMaps=false;
            bool doActivityImages=false;
            string maxCountStr = null;
            string AthleteId = null;
            string ActivityTypeStr = null;
            var p = new OptionSet () {
                { "s|maps",   v => { doMaps=true; } },
                { "ai|activity_images",   v => { doActivityImages=true; } },
                { "a|athleteid=",   v => { AthleteId=v; } },
                { "at|activity_type=",   v => { ActivityTypeStr=v; } },
                { "m|max_count=",   v => { maxCountStr=v; } },
            };
            p.Parse(args);
            int ret = -1;
            int maxCount=int.MaxValue;
            if (maxCountStr!=null)
            {
                maxCount=int.Parse(maxCountStr);
                logger.LogInformation($" limit download to {maxCount}");
            }
            try
            {
                using (StravaXApiContext db = new StravaXApiContext())
                {
                    IQueryable<ActivityShort> dbs = db.ActivityShortDB.OrderByDescending(b => b.ActivityDate);
                    if (ActivityTypeStr!=null)
                    {
                        ActivityType ActivityType=(ActivityType)Enum.Parse(typeof(ActivityType),ActivityTypeStr);
                        dbs=dbs.Where(a => a.ActivityType==ActivityType);
                    }

                    if (doMaps)
                    {
                        if (AthleteId!=null)    
                            ret = RetrieveMaps(dbs, logger, AthleteId, ActivityTypeStr, maxCount);
                        else
                        {
                            List<AthleteShort> athletes = db.AthleteShortDB.OrderBy(a => a.AthleteId).ToList();
                            foreach(AthleteShort athlete in athletes)
                            {
                                ret = RetrieveMaps(dbs, logger, athlete.AthleteId, ActivityTypeStr, maxCount);
                            }
                        }
                    }
                    if (doActivityImages)
                    {
                        if (AthleteId!=null)
                            ret = RetrieveActivityImages(dbs, logger, AthleteId, ActivityTypeStr, maxCount);
                        else
                        {
                            List<AthleteShort> athletes = db.AthleteShortDB.OrderBy(a => a.AthleteId).ToList();
                            foreach(AthleteShort athlete in athletes)
                            {
                                ret = RetrieveActivityImages(dbs, logger, athlete.AthleteId, ActivityTypeStr, maxCount);
                            }
                        }
                    }
                }
                ret = 0 ;
            }
            catch(Exception e)
            {
                logger.LogError($"ERROR:{e.ToString()}"); 
                ret = 1; 
            }
            return ret;
        }
        static private int RetrieveMaps(IQueryable<ActivityShort> dbs, ILogger logger, string AthleteId, string ActivityTypeStr, int maxCount)
        {
            WebClient webClient = new WebClient();
            var _dbs=dbs;
            if (AthleteId!=null)
                _dbs=_dbs.Where(a => a.AthleteId==AthleteId);
            _dbs.Where(a => a.ActivityImageMapUrl!=null);
            
            logger.LogDebug($"retrieve maps list");
            List<ActivityShort> activitiesWithMaps = _dbs.ToList();
            logger.LogDebug($"BEGIN maps for {(AthleteId==null?"all athletes":AthleteId)}/{(ActivityTypeStr==null?"all types":ActivityTypeStr)} :{activitiesWithMaps.Count()}");
            int imageCount=0;
            int skipCount=0;
            foreach(ActivityShort activity in activitiesWithMaps)
            {
                string ImageMapUrl = activity.ActivityImageMapUrl;
                if (ImageMapUrl==null)
                    continue;
                logger.LogDebug($"activity {activity.ActivityTitle} {ImageMapUrl}");
                string outputDir=$"maps/{activity.AthleteId}";
                string outputFilename=$"{activity.ActivityId}.png";
                if (!File.Exists($"{outputDir}/{outputFilename}"))
                {
                    if (!Directory.Exists(outputDir))
                    {
                        DirectoryInfo DirInfo = Directory.CreateDirectory(outputDir);
                        logger.LogDebug($"directory for maps created at {DirInfo.FullName}");
                    }
                    logger.LogDebug($"download {ImageMapUrl} in {outputDir}/{outputFilename}");
                    try
                    {
                        webClient.DownloadFile(ImageMapUrl,$"{outputDir}/{outputFilename}");
                        imageCount++;
                        if (imageCount%100==0)
                        {
                            logger.LogInformation($" images {imageCount}/{activitiesWithMaps.Count}");
                        }
                        if (imageCount>=maxCount) throw new Exception($"max count {imageCount}>={maxCount} reached");
                    }
                    catch(System.Net.WebException ex)
                    {
                        logger.LogError($"can't download {ImageMapUrl} in {outputDir}/{outputFilename}");
                        logger.LogError($"Message {ex.Message}");
                    }
                }
                else
                {
                    skipCount++;
                }
            }
            logger.LogInformation($"DONE maps read:{imageCount} skip:{skipCount} for {(AthleteId==null?"all athletes":AthleteId)}/{(ActivityTypeStr==null?"all types":ActivityTypeStr)} :{activitiesWithMaps.Count()}");
            return 0;
        }
        static private int RetrieveActivityImages(IQueryable<ActivityShort> dbs, ILogger logger, string AthleteId, string ActivityTypeStr, int maxCount)
        {
            WebClient webClient = new WebClient();
            var _dbs=dbs;
            if (AthleteId!=null)
                _dbs=_dbs.Where(a => a.AthleteId==AthleteId);
            _dbs.Where(a => a.ActivityImagesListAsString.Length>0);
            logger.LogInformation($"retrieve activity with image list");
            List<ActivityShort> activitiesWithImages = _dbs.ToList();
            logger.LogInformation($" activity with images {(AthleteId==null?"all athletes":AthleteId)}/{(ActivityTypeStr==null?"all types":ActivityTypeStr)} :{activitiesWithImages.Count()}");
            int imageCount=0;
            foreach(ActivityShort activity in activitiesWithImages)
            {
                List<string> ActivityImagesUrl = activity.ActivityImagesList;
                if (ActivityImagesUrl==null || ActivityImagesUrl.Count==0)
                    continue;
                int countInActivity=0;
                foreach(string imageUri in ActivityImagesUrl)
                {
                    countInActivity++;
                    logger.LogDebug($"activity {activity.ActivityTitle}{countInActivity}/{ActivityImagesUrl.Count} {imageUri}");
                    string outputDir=$"images/{activity.AthleteId}/{activity.ActivityId}";
                    string imageFilename=imageUri.Split("/").Last();
                    string outputFilename=imageFilename;
                    if (!imageFilename.ToLower().EndsWith(".jpg"))
                    {
                        outputFilename=$"{imageFilename}.jpg";
                    }
                    if (!File.Exists($"{outputDir}/{outputFilename}"))
                    {
                        if (!Directory.Exists(outputDir))
                        {
                            DirectoryInfo DirInfo = Directory.CreateDirectory(outputDir);
                            logger.LogDebug($"directory for maps created at {DirInfo.FullName}");
                        }
                        imageCount++;
                        if (imageCount%10==0)
                        {
                            logger.LogInformation($" images {imageCount}/{activitiesWithImages.Count}");
                        }
                        logger.LogDebug($"download {imageUri} in {outputDir}/{outputFilename}");
                        try
                        {
                            webClient.DownloadFile(imageUri,$"{outputDir}/{outputFilename}");
                        }
                        catch(System.Net.WebException ex)
                        {
                            logger.LogError($"can't download {imageUri} in {outputDir}/{outputFilename}");
                            logger.LogError($"Message {ex.Message}");
                        }
                    }
                    else{
                        logger.LogDebug($"skip {imageUri} in {outputDir}/{outputFilename}");
                    }
                }
                if (imageCount>=maxCount) throw new Exception($"max count {imageCount}>={maxCount} reached");
            }
            logger.LogInformation($"DONE images {imageCount}/{activitiesWithImages.Count}");
            return 0;
        }
    }
}

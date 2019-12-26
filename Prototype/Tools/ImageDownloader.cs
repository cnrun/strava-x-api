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
            if (maxCountStr!=null) {
                maxCount=int.Parse(maxCountStr);
            }
            try
            {
                using (StravaXApiContext db = new StravaXApiContext())
                {
                    IQueryable<ActivityShort> dbs = db.ActivityShortDB.OrderByDescending(b => b.ActivityDate);
                    if (AthleteId!=null)
                    {
                        dbs=dbs.Where(a => a.AthleteId==AthleteId);
                    }
                    if (ActivityTypeStr!=null)
                    {
                        ActivityType ActivityType=(ActivityType)Enum.Parse(typeof(ActivityType),ActivityTypeStr);
                        dbs=dbs.Where(a => a.ActivityType==ActivityType);
                    }

                    if (doMaps)
                    {
                        WebClient webClient = new WebClient();
                        List<ActivityShort> activitiesWithMaps = dbs.Where(a => a.ActivityImageMapUrl!=null).ToList();
                        logger.LogInformation($" activity with maps {(AthleteId==null?"all athletes":AthleteId)}/{(ActivityTypeStr==null?"all types":ActivityTypeStr)} :{activitiesWithMaps.Count()}");
                        int imageCount=0;
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
                                webClient.DownloadFile(ImageMapUrl,$"{outputDir}/{outputFilename}");
                                imageCount++;
                                if (imageCount%10==0)
                                {
                                    logger.LogInformation($" images {imageCount}/{activitiesWithMaps.Count}");
                                }
                                if (imageCount>=maxCount) break;
                            }
                        }
                        logger.LogInformation($"DONE images {imageCount}/{activitiesWithMaps.Count}");
                    }
                    if (doActivityImages)
                    {
                        WebClient webClient = new WebClient();
                        List<ActivityShort> activitiesWithImages = dbs.Where(a => a.ActivityImagesListAsString.Length>0).ToList();
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
                                    webClient.DownloadFile(imageUri,$"{outputDir}/{outputFilename}");
                                }
                                else{
                                    logger.LogDebug($"skip {imageUri} in {outputDir}/{outputFilename}");
                                }
                            }
                            if (imageCount>=maxCount) break;
                        }
                        logger.LogInformation($"DONE images {imageCount}/{activitiesWithImages.Count}");
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
    }
}

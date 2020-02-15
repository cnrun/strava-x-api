using Strava.XApi.Model;
using NDesk.Options;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Strava.XApi.Tools
{
    enum ExportType
    {
        HeatMap,
        TimeMap
    }

    public class GpxToKml
    {

        private static XNamespace gx="http://www.google.com/kml/ext/2.2";
        private static XNamespace kml = "http://www.opengis.net/kml/2.2";
        private static XNamespace atom = "http://www.w3.org/2005/Atom";
        /**
         * Convert GPX files to KML.
         * GPX File should have bee retrieved with get-gpx.
         * All GPX are merged in one KML file with subfolder for athlete and activity types.
         */
        public static int Convert(string[] args)
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
            logger.LogDebug("Log GpxToKml");
            string AthleteId = null;
            // default for now
            string ActivityTypeStr = null;
            string exportKmlPath = "StravaXApi.kml";
            string exportTypeStr = ExportType.HeatMap.ToString();
            string beginDateStr = null;
            var p = new OptionSet () {
                { "a|athleteid=",   v => { AthleteId=v; } },
                { "at|activity_type=",   v => { ActivityTypeStr=v; } },
                { "e|export_type=",   v => { exportTypeStr=v; } },
                { "bd|begin_date=",   v => { beginDateStr=v; } },
                { "d|destination=",   v => { exportKmlPath=v; } },
            };
            p.Parse(args);
            int ret = -1;

            ExportType exportType=(ExportType)Enum.Parse(typeof(ExportType), exportTypeStr);

            try
            {
                List<ActivityShort> activities;
                using (StravaXApiContext db = new StravaXApiContext())
                {
                    IQueryable<ActivityShort> dbs = db.ActivityShortDB.OrderBy(a=>a.AthleteId).ThenBy(a=>a.ActivityType).ThenByDescending(b=>b.ActivityDate);
                    if (ActivityTypeStr!=null)
                    {
                        ActivityType ActivityType=(ActivityType)Enum.Parse(typeof(ActivityType),ActivityTypeStr);
                        dbs=dbs.Where(a => a.ActivityType==ActivityType);
                    }
                    if (AthleteId!=null)
                    {
                        dbs=dbs.Where(a => a.AthleteId==AthleteId);
                    }
                    if (beginDateStr!=null)
                    {
                        DateTime dateTime=DateTime.Parse(beginDateStr);
                        Console.WriteLine($"filter for date {dateTime}");
                        dbs=dbs.Where(a => a.ActivityDate>=dateTime);
                    }
                    // Ignore Activities without image map. They have probably been enterred without gps-track.
                    dbs=dbs.Where(a => a.ActivityImageMapUrl!=null);
                    activities = dbs.ToList();

                    logger.LogInformation($"BEGIN GPX➡️KML {(AthleteId==null?"all athletes":AthleteId)}/{(ActivityTypeStr==null?"all types":ActivityTypeStr)}/{(beginDateStr==null?"all dates":DateTime.Parse(beginDateStr).ToString())} :{activities.Count()}");
                    string lastAthleteId=null;
                    ActivityType lastActivityType=ActivityType.Other;
                    XElement StravaXApiFolder=new XElement(kml+"Folder"
                        ,new XElement(kml+"name","Strava-X-Api")
                        ,new XElement(kml+"open","1")
                    );
                    XElement currentAthleteFolder=null;
                    XElement currentActivityTypeFolder=null;
                    int Count=0;
                    // go throw all activities, sorted by athlete / activity type / activity date
                    // <visibility>0</visibility>
                    foreach(ActivityShort activity in activities)
                    {
                        if (lastAthleteId==null)
                        {
                            // first round init athlete
                            AthleteShort athlete = db.AthleteShortDB.Find(activity.AthleteId);
                            string athleteName=athlete.AthleteName;
                            currentAthleteFolder=new XElement(kml+"Folder"
                                ,new XElement(kml+"name",athleteName)
                                ,new XElement(kml+"visibility","0")
                                ,new XElement(kml+"open","0"));
                            StravaXApiFolder.Add(currentAthleteFolder);
                            lastAthleteId=activity.AthleteId;
                            // first round init activity
                            currentActivityTypeFolder=new XElement(kml+"Folder"
                                ,new XElement(kml+"name",activity.ActivityType.ToString())
                                ,new XElement(kml+"open","0"));
                            currentAthleteFolder.Add(currentActivityTypeFolder);
                            lastActivityType=activity.ActivityType;
                        }
                        else
                        {
                            if (lastAthleteId==activity.AthleteId)
                            {
                                // same athlete, check activity type
                                if (lastActivityType!=activity.ActivityType)
                                {
                                    // start a new activity folder
                                    currentActivityTypeFolder=new XElement(kml+"Folder"
                                        ,new XElement(kml+"name",activity.ActivityType.ToString())
                                        ,new XElement(kml+"open","0"));
                                    currentAthleteFolder.Add(currentActivityTypeFolder);
                                    lastActivityType=activity.ActivityType;
                                }
                            }
                            else
                            {
                                // start a new athlete folder
                                AthleteShort athlete = db.AthleteShortDB.Find(activity.AthleteId);
                                string athleteName=athlete.AthleteName;
                                currentAthleteFolder=new XElement(kml+"Folder"
                                    ,new XElement(kml+"name",athleteName)
                                    ,new XElement(kml+"visibility","0")
                                    ,new XElement(kml+"open","0"));
                                StravaXApiFolder.Add(currentAthleteFolder);
                                // start a new activity folder
                                currentActivityTypeFolder=new XElement(kml+"Folder"
                                    ,new XElement(kml+"name",activity.ActivityType.ToString())
                                    ,new XElement(kml+"open","0"));
                                currentAthleteFolder.Add(currentActivityTypeFolder);
                                lastAthleteId=activity.AthleteId;
                            }
                        }

                        string outputDir=$"gpx/{activity.AthleteId}";
                        string outputFilename=$"{activity.ActivityId}_{activity.AthleteId}.gpx";
                        string outputFilenameGz=$"{activity.ActivityId}_{activity.AthleteId}.gpx.gz";
                        string outputFilenameErr=$"{activity.ActivityId}_{activity.AthleteId}.err";

                        try
                        {
                            if (File.Exists($"{outputDir}/{outputFilenameGz}"))
                            {
                                using(FileStream compressedFileStream = File.OpenRead($"{outputDir}/{outputFilenameGz}"))
                                {
                                    using (GZipStream compressionStream = new GZipStream(compressedFileStream, CompressionMode.Decompress))
                                    {
                                        currentActivityTypeFolder.Add(handleGpxStream(compressionStream, exportType, activity));
                                    }
                                } 
                            }
                            else if (File.Exists($"{outputDir}/{outputFilename}"))
                            {
                                using(FileStream fileStream = File.OpenRead($"{outputDir}/{outputFilename}"))
                                {
                                    currentActivityTypeFolder.Add(handleGpxStream(fileStream, exportType, activity));
                                } 
                            }
                            else if (File.Exists($"{outputDir}/{outputFilenameErr}"))
                            {
                                // Skip error file.
                                logger.LogWarning($"SKIP: GPX not availlable for {activity.ActivityId} {lastAthleteId} {lastActivityType}");
                            }
                        }
                        catch(Exception e)
                        {
                            logger.LogWarning($"SKIP: Can't parse GPX for {activity.ActivityId} {lastAthleteId} {lastActivityType} err:{e.Message}");
                        }

                        if (Count++%100==0)
                        {
                            Console.WriteLine($"{Count}/{activities.Count()}");
                        }
                    }
                    XDocument kmlDoc = new XDocument(
                        new XDeclaration("1.0", "UTF-8", "yes"),
                        new XElement(kml+"kml"
                        ,new XAttribute("xmlns", kml)
                        ,new XAttribute(XNamespace.Xmlns+"gx",gx)
                        ,new XAttribute(XNamespace.Xmlns+"kml", kml)
                        ,new XAttribute(XNamespace.Xmlns+"atom", atom)
                        ,StravaXApiFolder));
                    kmlDoc.Save(exportKmlPath);
                    logger.LogInformation($"exported file at {exportKmlPath} ");
                }
                ret = 0;
            }
            catch(Exception e)
            {
                logger.LogError($"ERROR:{e.ToString()}");
                ret = 1;
            }
            return ret;

        }

        private static XElement handleGpxStream(Stream stream, ExportType exportType, ActivityShort activity)
        {
            XElement GpxToKmlElt;
            switch(exportType)
            {
                case ExportType.HeatMap:
                    // for heatmap without time.
                    GpxToKmlElt = readGpx(activity, stream,kml);
                    break;
                case ExportType.TimeMap:
                    // for heatmap with time.
                    GpxToKmlElt = convertToKmlWithTime(activity, stream, kml, gx);
                    break;
                default:
                    throw new System.InvalidOperationException($"Export type not supported {exportType}");
            }
            return GpxToKmlElt;
        }
        /**
         * Extract all points from the gpx file and fill an XElement which could be integrate in the kml file.
         */
        public static XElement readGpx(ActivityShort activity, Stream stream,XNamespace kml)
        {
            XDocument gpxDoc = XDocument.Load(stream);

            XNamespace ns = "{http://www.topografix.com/GPX/1/1}";
            var gpxElt=gpxDoc.Root;
            var trkptElts=gpxElt.Element($"{ns}trk").Element($"{ns}trkseg").Elements();
            StringBuilder sb = new StringBuilder();
            foreach (XElement element in trkptElts)
            {
                // Console.WriteLine($" {element}");
                var lat=element.Attribute("lat").Value;
                var lon=element.Attribute("lon").Value;
                var ele=element.Element($"{ns}ele").Value;
                if (sb.Length>0) sb.Append(" ");
                sb.Append($"{lon},{lat},{ele}");
            }

            var kmlGpxDocument = new XElement(kml+"Document"
                ,new XElement(kml+"name", activity.ActivityTitle)
                ,new XElement(kml+"Style", new XAttribute("id","lineStyle")
                    ,new XElement(kml+"LineStyle",
                        new XElement(kml+"color","80ffac59"),
                        new XElement(kml+"width","6" )
                ))
                ,new XElement(kml+"Placemark"
                    ,new XElement(kml+"name","Path")
                    ,new XElement(kml+"visibility","0")
                    ,new XElement(kml+"styleUrl","#lineStyle")
                    ,new XElement(kml+"LineString",
                        new XElement(kml+"tessellate","1"),
                        new XElement(kml+"coordinates",sb.ToString())
            )));
            return kmlGpxDocument;

        }
        public static XElement convertToKmlWithTime(ActivityShort activity, Stream stream,XNamespace kml,XNamespace gx)
        {
            XDocument gpxDoc = XDocument.Load(stream);

            XNamespace ns = "{http://www.topografix.com/GPX/1/1}";
            var gpxElt=gpxDoc.Root;
            var trkptElts=gpxElt.Element($"{ns}trk").Element($"{ns}trkseg").Elements();

            List<XElement> whenList = new List<XElement>();
            List<XElement> coordList = new List<XElement>();

            // start time of the activity
            var startTime = activity.ActivityDate;
            // end time of the activity
            var activityTime = Utils.extractActivityTime(activity);
            var endTime = activity.ActivityDate.Add(activityTime);

            int pointCount=trkptElts.Count();
            // double deltaTimeMs = (activityTime.Milliseconds);
            int currentPointIndex=1;
            foreach (XElement element in trkptElts)
            {
                // Console.WriteLine($" {element}");
                var lat=element.Attribute("lat").Value;
                var lon=element.Attribute("lon").Value;
                var ele=element.Element($"{ns}ele").Value;

                // 2020-02-09T11:45:03Z
                var whenPoint = startTime.AddSeconds(((double)activityTime.TotalSeconds)*((double)currentPointIndex/(double)pointCount));
                XElement whendElt = new XElement(kml+"when",whenPoint.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                whenList.Add(whendElt);

                XElement coordElt = new XElement(gx+"coord",$"{lon} {lat} {ele}");
                coordList.Add(coordElt);
                currentPointIndex++;
            }

            var kmlGpxDocument = new XElement(kml+"Document"
                ,new XElement(kml+"name", activity.ActivityTitle)
                ,new XElement(kml+"Style", new XAttribute("id","multiTrack_h")
                    ,new XElement(kml+"IconStyle",
                        new XElement(kml+"scale","1.2"),
                        new XElement(kml+"Icon",
                            new XElement(kml+"href","http://earth.google.com/images/kml-icons/track-directional/track-0.png")))
                    ,new XElement(kml+"LineStyle",
                        new XElement(kml+"color","99ffac59"),
                        new XElement(kml+"width","8" )
                ))
                ,new XElement(kml+"StyleMap", new XAttribute("id","multiTrack")
                    ,new XElement(kml+"Pair",
                        new XElement(kml+"key","normal"),
                        new XElement(kml+"styleUrl","#multiTrack_n")
                    )
                    ,new XElement(kml+"Pair",
                        new XElement(kml+"key","highlight"),
                        new XElement(kml+"styleUrl","#multiTrack_h")
                    )
                )
                ,new XElement(kml+"Style", new XAttribute("id","multiTrack_n")
                    ,new XElement(kml+"IconStyle",
                        new XElement(kml+"Icon",
                            new XElement(kml+"href","http://earth.google.com/images/kml-icons/track-directional/track-0.png")))
                    ,new XElement(kml+"LineStyle",
                        new XElement(kml+"color","99ffac59"),
                        new XElement(kml+"width","6" )
                ))
                ,new XElement(kml+"Placemark"
                    ,new XElement(kml+"name",activity.ActivityTitle)
                    ,new XElement(kml+"visibility","0")
                    ,new XElement(kml+"styleUrl","#multiTrack")
                    ,new XElement(gx+"Track",
                        whenList,
                        coordList
            )));
            return kmlGpxDocument;

        }
    }
}

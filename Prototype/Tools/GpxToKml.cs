using Prototype.Model;
using NDesk.Options;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Prototype.Tools
{    
    public class GpxToKml
    {
        /**
         * Convert GPX files to KML.
         * GPX File should have bee retrieved with get-gpx.
         * All GPX would be merged in one KML file with subfolder for athlete and activity types.
         */
        public static int Convert(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", Microsoft.Extensions.Logging.LogLevel.Warning)
                    .AddFilter("System", Microsoft.Extensions.Logging.LogLevel.Warning)
                    .AddFilter("Prototype.Tools.GpxDownloader", Microsoft.Extensions.Logging.LogLevel.Debug)
                    .AddFilter("StravaXApi", Microsoft.Extensions.Logging.LogLevel.Information)
                    .AddProvider(new CustomLoggerProvider());
                    //.AddEventLog();
            });
            var logger = loggerFactory.CreateLogger<StravaXApi>();
            logger.LogDebug("Log GpxToKml");
            string AthleteId = null;
            // default for now
            string ActivityTypeStr = ActivityType.BackcountrySki.ToString();
            var p = new OptionSet () {
                { "a|athleteid=",   v => { AthleteId=v; } },
                { "at|activity_type=",   v => { ActivityTypeStr=v; } },
            };
            p.Parse(args);
            int ret = -1;

            XNamespace gx="http://www.google.com/kml/ext/2.2";
            XNamespace kml = "http://www.opengis.net/kml/2.2";
            XNamespace atom = "http://www.w3.org/2005/Atom";

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
                    // Ignore Activities without image map. They have probably been enterred without gps-track.
                    dbs=dbs.Where(a => a.ActivityImageMapUrl!=null);
                    activities = dbs.ToList();

                    logger.LogInformation($"BEGIN GPX➡️KML {(AthleteId==null?"all athletes":AthleteId)}/{(ActivityTypeStr==null?"all types":ActivityTypeStr)} :{activities.Count()}");
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
                    foreach(ActivityShort activity in activities)
                    {
                        if (lastAthleteId==null)
                        {
                            // first round init athlete
                            AthleteShort athlete = db.AthleteShortDB.Find(activity.AthleteId);                    
                            string athleteName=athlete.AthleteName;
                            currentAthleteFolder=new XElement(kml+"Folder"
                                ,new XElement(kml+"name",athleteName)
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
                        if (File.Exists($"{outputDir}/{outputFilename}"))
                        {
                            try
                            {
                                XElement GpxToKmlElt = readGpx(activity, $"{outputDir}/{outputFilename}",kml);
                                currentActivityTypeFolder.Add(GpxToKmlElt);
                            }
                            catch(Exception)
                            {
                               logger.LogWarning($"SKIP: {outputFilename} {lastAthleteId} {lastActivityType}"); 
                            }
                            // Console.WriteLine($"add GPX to {outputFilename} {lastAthleteId} {lastActivityType}");
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
                        ,new XAttribute(XNamespace.Xmlns + "kml", kml)
                        ,new XAttribute(XNamespace.Xmlns + "atom", atom)
                        ,StravaXApiFolder));
                    kmlDoc.Save("/Users/ericlouvard/Downloads/StravaXApi.kml");
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
        /**
         * Extract all points from the gpx file and fill an XElement which could be integrate in the kml file.
         */
        public static XElement readGpx(ActivityShort activity, string filepath,XNamespace kml)
        {
            XDocument gpxDoc = XDocument.Load(filepath);
            
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
                    ,new XElement(kml+"styleUrl","#lineStyle")
                    ,new XElement(kml+"LineString",
                        new XElement(kml+"tessellate","1"),
                        new XElement(kml+"coordinates",sb.ToString())
            )));
            return kmlGpxDocument;

        }
    }
}

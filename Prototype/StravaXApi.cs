using System;
using System.Threading;
using System.Globalization;
using System.IO;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Remote;
using System.Security;
using Prototype.Model;
using System.Collections.Generic;
namespace Prototype
{
    class StravaXApi: IDisposable
    {
        private IWebDriver BrowserDriver;
        private Boolean ScreenshotsMonthActivities = false;
        private Boolean DownloadThumbnailsActivities = false;
        private Boolean DownloadImagesActivities = false;
        private Boolean VerboseDebug = false;
        private Boolean RunBrowserStack = true;
        private string Username;
        private SecureString Password;
        static void Main(string[] args)
        {
            ReadActivitiesForAthlete(args);
        }

        static StravaXApi GetStravaXApi(string[] args)
        {
            String Username = Environment.GetEnvironmentVariable("STRAVA_USER");
            SecureString Password = new SecureString();
            foreach(var c in Environment.GetEnvironmentVariable("STRAVA_PWD"))
                Password.AppendChar(c);
            Password.MakeReadOnly();
            StravaXApi stravaXApi;
            if (Environment.GetEnvironmentVariable("BROWSERSTACK")=="ON")
                stravaXApi = new StravaXApi(Environment.GetEnvironmentVariable("BROWSERSTACK_USER"),Environment.GetEnvironmentVariable("BROWSERSTACK_PWD"));
            else
                stravaXApi = new StravaXApi();
            
            stravaXApi.Username = Username;
            stravaXApi.Password = Password;
            stravaXApi.ScreenshotsMonthActivities = Array.IndexOf(args,"--ScreenshotsMonthActivities") >= 0;
            stravaXApi.DownloadThumbnailsActivities = Array.IndexOf(args,"--DownloadThumbnailsActivities") >= 0;
            stravaXApi.DownloadImagesActivities = Array.IndexOf(args,"--DownloadImagesActivities") >= 0;
            stravaXApi.VerboseDebug = Array.IndexOf(args,"--VerboseDebug") >= 0;
            stravaXApi.RunBrowserStack = Array.IndexOf(args,"--RunBrowserStack") >= 0;
            return stravaXApi;
        }
        static void ReadAthleteConnectionsForAthlete(string[] args)
        {
            Console.WriteLine("Read athlete connections with Strava-X-API.");
            if (args.Length < 1)
            {
                Console.WriteLine("Please find the needed arguments from the code 😛. Oh there are several options with environment variables! ");
                return;
            }

            String AthleteId = args[0];
            StravaXApi stravaXApi = GetStravaXApi(args);

            try
            {
                stravaXApi.signIn();
                var AthleteShortList = stravaXApi.getConnectedAthetes(AthleteId);
            }
            catch(Exception e)
            {
                Console.WriteLine($"ERROR:{e.ToString()}");  
            }
            finally
            {
                stravaXApi.Dispose();
            }
        }
        static void ReadActivitiesForAthlete(string[] args)
        {
            Console.WriteLine("Read athlete activities with Strava-X-API.");
            if (args.Length < 1)
            {
                Console.WriteLine("Please find the needed arguments from the code 😛. Oh there are several options with environment variables! ");
                return;
            }

            String AthleteId = args[0];
            StravaXApi stravaXApi = GetStravaXApi(args);

            try
            {
                stravaXApi.signIn();
                List<ActivityShort> ActivitiesList = new List<ActivityShort>();

                DateTime FirstActivityDate = stravaXApi.getActivityRange(AthleteId);
                System.Console.WriteLine($"First activity at {FirstActivityDate.Year}/{FirstActivityDate.Month}");                    

                int FromYear=int.Parse(Environment.GetEnvironmentVariable("FROM_YEAR"));
                int FromMonth=int.Parse(Environment.GetEnvironmentVariable("FROM_MONTH"));
                int ToYear=int.Parse(Environment.GetEnvironmentVariable("TO_YEAR"));
                int ToMonth=int.Parse(Environment.GetEnvironmentVariable("TO_MONTH"));
                DateTime now = DateTime.Now;
                for(int year=FromYear;year<=ToYear;year++)
                {
                    for(int month=FromMonth;month<=ToMonth;month++)
                    {
                        List<ActivityShort> ActivitiesMonthList;
                        try
                        {
                            ActivitiesMonthList = stravaXApi.getActivities(AthleteId,$"{year:D4}",$"{month:D2}");
                        }
                        catch(StaleElementReferenceException)
                        {
                            // Wait and try again.
                            Thread.Sleep(2000);
                            ActivitiesMonthList = stravaXApi.getActivities(AthleteId,$"{year:D4}",$"{month:D2}");
                        }
                        ActivitiesList.AddRange(ActivitiesMonthList);
                        using (StravaXApiContext db = new StravaXApiContext())
                        {
                            foreach(ActivityShort ActivityShort in ActivitiesList)
                            {
                                Console.WriteLine($"JSON={ActivityShort.SerializePrettyPrint(ActivityShort)}");
                                if (db.ActivityShortDB.Find(ActivityShort.ActivityId)==null)
                                {
                                    db.ActivityShortDB.Add(ActivityShort);
                                    db.SaveChanges();
                                    Console.WriteLine($"Enterred Activities: {db.ActivityShortDB.OrderBy(b => b.ActivityId).Count()}");
                                }
                                else
                                {
                                    Console.WriteLine($"{ActivityShort.ActivityId} allready in database");
                                }
                            }
                            Console.WriteLine($"total read = {ActivitiesList.Count}");
                            Console.WriteLine($"total stored = {db.ActivityShortDB.OrderBy(b => b.ActivityId).Count()}");
                            ActivitiesList.Clear();
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine($"ERROR:{e.ToString()}");  
            }
            finally
            {
                stravaXApi.Dispose();
            }
        }

        StravaXApi()
        {
            ChromeOptions Options = new ChromeOptions();
            Options.AddArgument("--window-size=1300,15000");
            Options.AddArgument("--headless");            
            BrowserDriver = new ChromeDriver(Options);
        }
        StravaXApi(string BrowserStackUserName, string BrowserStackAccessKey)
        {
            // Warnings do not make sence for RemoteWebDriver with BrowserStack
            #pragma warning disable CS0618
            DesiredCapabilities capability = new DesiredCapabilities();
            // capability.SetCapability("single", "System.Configuration.AppSettingsSection, System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            // capability.SetCapability("local", "System.Configuration.AppSettingsSection, System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            capability.SetCapability("parallel", "System.Configuration.AppSettingsSection, System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            capability.SetCapability("chrome", "System.Configuration.AppSettingsSection, System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            capability.SetCapability("firefox", "System.Configuration.AppSettingsSection, System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            capability.SetCapability("safari", "System.Configuration.AppSettingsSection, System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            capability.SetCapability("ie","System.Configuration.AppSettingsSection, System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

            capability.SetCapability("build", "version1");
            capability.SetCapability("project", "strava-x-api");

            capability.SetCapability("browserstack.user", BrowserStackUserName);
            capability.SetCapability("browserstack.key", BrowserStackAccessKey);
            capability.SetCapability("os", "Windows");
            capability.SetCapability("os_version", "10");
            capability.SetCapability("browser_version", "78.0");
            capability.SetCapability("resolution", "2048x1536"); //2048x1536
            
            #pragma warning restore CS0618

            BrowserDriver = new RemoteWebDriver(new Uri("http://hub-cloud.browserstack.com/wd/hub/"), capability);
            BrowserDriver.Manage().Window.Maximize();
        }
        public void signIn()
        {
            String url = $"https://www.strava.com/login";
            BrowserDriver.Navigate().GoToUrl(url);
            // Enter login data
            BrowserDriver.FindElement(By.Name("email")).SendKeys(Username);
            BrowserDriver.FindElement(By.Name("password")).SendKeys(new System.Net.NetworkCredential("", Password).Password);
            BrowserDriver.FindElement(By.Id("login-button")).Click();
            // Wait until Login is done.
            new WebDriverWait(BrowserDriver, TimeSpan.FromSeconds(30)).Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists((By.XPath("//div[@class='media']"))));
        }

        /**
         * Find the oldest possible date for an activity
         */
        public DateTime getActivityRange(String AthleteId)
        {
            // open athlete page
            String url = $"https://www.strava.com/athletes/{AthleteId}";

            BrowserDriver.Navigate().GoToUrl(url);            
            Console.WriteLine($"open ${url}");

            // locate dates in the pulldown menu
            var Elts=BrowserDriver.FindElements(By.XPath("//div[@id='interval-graph']/div/div/ul/li/a"));
            int MinDateInt = int.MaxValue;
            String MinDateString="";
            // parse all entries
            foreach (IWebElement Elt in Elts)
            {
                string UrlString = Elt.GetAttribute("href");
                int index = UrlString.IndexOf("interval=");
                string DateString = UrlString.Substring(index+9,6);
                
                int ActDateInt = int.Parse(DateString);
                // extract the oldest date
                if (ActDateInt<MinDateInt)
                {
                    MinDateInt = ActDateInt;
                    MinDateString=DateString;
                }
            }
            if (string.IsNullOrEmpty(MinDateString)) throw new ArgumentException("can't find date interval");
            // compute the date from the week number
            int YearBeginInt=int.Parse(MinDateString.Substring(0,4))-1;
            int WeekBeginInt=int.Parse(MinDateString.Substring(4,2));
            DateTime FirstActivityDate = FirstDateOfWeekISO8601(YearBeginInt,WeekBeginInt);
            if (VerboseDebug) System.Console.WriteLine($"Begin = {YearBeginInt}/{WeekBeginInt} {FirstActivityDate}");

            return FirstActivityDate;
        }
        /**
         * from https://stackoverflow.com/a/9064954/281188
         */
        public static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            // Use first Thursday in January to get first week of the year as
            // it will never be in Week 52/53
            DateTime firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = weekOfYear;
            // As we're adding days to a date in Week 1,
            // we need to subtract 1 in order to get the right date for week #1
            if (firstWeek == 1)
            {
                weekNum -= 1;
            }

            // Using the first Thursday as starting week ensures that we are starting in the right year
            // then we add number of weeks multiplied with days
            var result = firstThursday.AddDays(weekNum * 7);

            // Subtract 3 days from Thursday to get Monday, which is the first weekday in ISO8601
            return result.AddDays(-3);
        }  
        public List<AthleteShort> getConnectedAthetes(String AthleteId)
        {
            String url = $"https://www.strava.com/athletes/{AthleteId}";

            BrowserDriver.Navigate().GoToUrl(url);            
            Console.WriteLine($"open ${url}");

            List<AthleteShort> AthleteShortList = new List<AthleteShort>();
            return AthleteShortList;
        }

        public List<ActivityShort> getActivities(String AthleteId, String Year, String Month)
        {
            String url = $"https://www.strava.com/athletes/{AthleteId}#interval_type?chart_type=miles&interval_type=month&interval={Year}{Month}&year_offset=0";

            BrowserDriver.Navigate().GoToUrl(url);            
            Console.WriteLine($"open ${url}");
            DateTime CrawlDate = DateTime.Now;
            // Should wait for element.
            Thread.Sleep(2000);

            if (!Directory.Exists("./screenshots"))
            {
                DirectoryInfo DirInfo = Directory.CreateDirectory("./screenshots");
                Console.WriteLine($"directory for screenshots created at {DirInfo.FullName}");
            }
            if (ScreenshotsMonthActivities)
            {
                ((ITakesScreenshot)BrowserDriver).GetScreenshot().SaveAsFile($"./screenshots/{AthleteId}_{Year}_{Month}.png");
            }

            // Find all activity icons in thos page
            var Elts=BrowserDriver.FindElements(By.XPath("//div[@class='entry-type-icon']"));
            if (VerboseDebug) Console.WriteLine($"Elts count={Elts.Count} for {Year}/{Month}");

            List<ActivityShort> ActivitiesList = new List<ActivityShort>();

            foreach (IWebElement Elt in Elts)
            {
                try{
                    // locate the div for activity number
                    var ActivityNumberElt = Elt.FindElement(By.XPath("./../../.."));
                    var ActivityId=ActivityNumberElt.GetAttribute("id");                

                    // for activitty with picture we have to search on step higher
                    if (String.IsNullOrEmpty(ActivityId)) {
                        ActivityNumberElt = ActivityNumberElt.FindElement(By.XPath("./.."));
                        ActivityId=ActivityNumberElt.GetAttribute("id");
                    }
                    ActivityId = ActivityId.Substring("Activity-".Length);
                    // activity title
                    var ActivityTitleElt = ActivityNumberElt.FindElement(By.XPath(".//strong/a"));
                    var ActivityTitle = ActivityTitleElt.Text;

                    // activity thumbnails images 
                    var ActivityImageElts = ActivityNumberElt.FindElements(By.XPath(".//img[@alt='Photo']"));
                    List<String> ActivityThumbnailsList = new List<String>();
                    foreach (IWebElement ActivityImageElt in ActivityImageElts)
                    {
                        // https://dgtzuqphqg23d.cloudfront.net/Wz2CrhzkXF3hm99lZmgWRBRbWhHBPLxUGDja_aMJDeQ-128x72.jpg
                        string imageUrl = ActivityImageElt.GetAttribute("src");
                        ActivityThumbnailsList.Add(imageUrl);                        
                        if (VerboseDebug) System.Console.WriteLine($"Activity {ActivityId} url {imageUrl}");
                        if(DownloadThumbnailsActivities)
                        {
                            System.Net.WebClient webClient = new System.Net.WebClient();
                            string[] pathElts = imageUrl.Split('/');
                            string localFileName = $"./screenshots/{AthleteId}_{Year}_{Month}_{ActivityId}_{pathElts[pathElts.Length-1]}.png";
                            webClient.DownloadFile(imageUrl, localFileName);
                        }
                    }
                    
                    // activity big images
                    var ActivityBigImageElts = ActivityNumberElt.FindElements(By.XPath(".//li[@str-type='photo']"));
                    List<String> ActivityImagesList = new List<String>();
                    foreach (IWebElement ActivityImageElt in ActivityBigImageElts)
                    {
                        // https://dgtzuqphqg23d.cloudfront.net/Wz2CrhzkXF3hm99lZmgWRBRbWhHBPLxUGDja_aMJDeQ-2048x1152.jpg
                        string imageUrl = ActivityImageElt.GetAttribute("str-target-url");
                        ActivityImagesList.Add(imageUrl);                        
                        if (VerboseDebug) System.Console.WriteLine($"Activity {ActivityId} url {imageUrl}");
                        if(DownloadImagesActivities)
                        {
                            System.Net.WebClient webClient = new System.Net.WebClient();
                            string[] pathElts = imageUrl.Split('/');
                            string localFileName = $"./screenshots/{AthleteId}_{Year}_{Month}_{ActivityId}_{pathElts[pathElts.Length-1]}.png";
                            webClient.DownloadFile(imageUrl, localFileName);
                        }
                    }

                    // Locate activity time information
                    string ActivityTimeString = "";
                    IWebElement ActivityTimeElt;
                    string AthleteIdInGroup = AthleteId;
                    List<String> GroupActivityList = new List<String>();
                    List<String> GroupAthleteList = new List<String>();
                    try{
                        if (ActivityNumberElt.TagName == "li")
                        {
                            // because of group activities I need to go to parents higher.
                            ActivityTimeElt = ActivityNumberElt.FindElement(By.XPath("./../..//time[@class='timestamp']"));
                            // find out which Athlete Id
                            var AthleteIdInGroupElt = ActivityNumberElt.FindElement(By.XPath(".//a[contains(@href,'/athletes/')]"));
                            AthleteIdInGroup = AthleteIdInGroupElt.GetAttribute("href");
                            AthleteIdInGroup = AthleteIdInGroup.Substring(AthleteIdInGroup.LastIndexOf("/")+1);
                            if (VerboseDebug) System.Console.WriteLine($"Groupped activity : Activity {ActivityId} Athlete {AthleteIdInGroup}");

                            // for group activities we are creating groups                            
                            var GroupActivityElts = ActivityNumberElt.FindElements(By.XPath("./../../..//li[@class='entity-details feed-entry']"));
                            foreach (IWebElement GroupActivityElt in GroupActivityElts)
                            {
                                string GroupActivityString=GroupActivityElt.GetAttribute("id");
                                GroupActivityString = GroupActivityString.Substring("Activity-".Length);
                                if (ActivityId!=GroupActivityString)
                                {
                                    // in ActivitysGroupElts we have both the original activity and the other activities in the group. We need to filter
                                    GroupActivityList.Add(GroupActivityString);
                                    // also retrieve the athlete id for the activity
                                    string GroupAthleteUrl = GroupActivityElt.FindElement(By.XPath(".//a[@class='avatar-content']")).GetAttribute("href");
                                    string GroupAthleteId = GroupAthleteUrl.Substring(GroupAthleteUrl.LastIndexOf('/')+1);
                                    GroupAthleteList.Add(GroupAthleteId);
                                    if (VerboseDebug) Console.WriteLine($"Group {ActivityId} with {GroupActivityString} from {GroupAthleteId}");
                                }
                            }
                        }
                        else
                        {
                            ActivityTimeElt = ActivityNumberElt.FindElement(By.XPath(".//time[@class='timestamp']"));
                        }
                        ActivityTimeString = ActivityTimeElt.GetAttribute("datetime");
                    }
                    catch(WebDriverException e) {
                        throw new NotFoundException($"can't read activity time {ActivityId} at {url} Err:{e.Message}", e);
                    }

                    string ActivityImageMapUrl;
                    try{
                        IWebElement ImageWithMapElt;
                        if (ActivityNumberElt.TagName == "li")
                        {
                            // Image with map for activities with group
                            ImageWithMapElt = ActivityNumberElt.FindElement(By.XPath("./../../div/a/div[contains(@str-type,'map')]/img"));
                        }
                        else
                        {
                            // Image with map for activities without group
                            ImageWithMapElt = ActivityNumberElt.FindElement(By.XPath(".//a[contains(@str-type,'map')]/img"));
                        }
                        ActivityImageMapUrl=ImageWithMapElt.GetAttribute("src");
                    }
                    catch(WebDriverException) {
                        // activity map is not always present.
                        ActivityImageMapUrl=null;
                    }

                    // Retrieve the activity class, with that it's poosible to know the activity type
                    var ActivityTypeElt = Elt.FindElement(By.XPath("./span/span"));
                    ActivityType ActivityType = parseActivityType(ActivityTypeElt.GetAttribute("class"));

                    DateTime ActivityTime = DateTime.Parse(ActivityTimeString.Substring(0,ActivityTimeString.Length-4));
                    Console.WriteLine($"Id={ActivityId} Text={ActivityTitle} Type={ActivityType} Time={ActivityTime}");                    
                    var ActivityShort = new ActivityShort();
                    ActivityShort.AthleteId = AthleteId;
                    ActivityShort.ActivityId = ActivityId;
                    ActivityShort.ActivityTitle = ActivityTitle;
                    ActivityShort.ActivityType = ActivityType;
                    ActivityShort.ActivityDate = ActivityTime;
                    ActivityShort.ActivityLastCrawled = CrawlDate;
                    ActivityShort.ActivityImageMapUrl=ActivityImageMapUrl;
                    ActivityShort.ActivityThumbnailsList = ActivityThumbnailsList;
                    ActivityShort.ActivityImagesList = ActivityImagesList;
                    ActivityShort.GroupActivityList = GroupActivityList;
                    ActivityShort.GroupAthleteList = GroupAthleteList;

                    ActivitiesList.Add(ActivityShort);
                }
                catch (Exception e) when (e is WebDriverException || e is NotFoundException)
                {
                    if (e is InvalidElementStateException || e is StaleElementReferenceException)
                    {
                        // Page seams to be incorrect loaded. Probably need to wait more.
                        throw e;
                    }
                    Console.WriteLine($"Skip Activity at {url} Err:{e.Message}");
                }
            }
            return ActivitiesList;
        }
        private ActivityType parseActivityType(string ActivityTypeCssClass)
        {
            // Extract css class with activity type from all classes.
            string ActivityTypeString = ActivityTypeCssClass.Substring(14);
            ActivityTypeString = ActivityTypeString.Substring(0,ActivityTypeString.IndexOf(' '));
            // Convert to ActivityType with the same text.
            ActivityType ret = (ActivityType)Enum.Parse(typeof(ActivityType),ActivityTypeString,true);
            if (! Enum.IsDefined(typeof(ActivityType), ret) | ret.ToString().Contains(","))  
            {
                // Fallback when ActivityType is not in Enum
                ret = ActivityType.Other;
                Console.WriteLine("{0} is not an underlying value of the ActivityType enumeration.", ActivityTypeString);
            }
            return ret;
        }
        public void Dispose()
        {
            // Close Browser.
            BrowserDriver.Quit();
        }
    }
}

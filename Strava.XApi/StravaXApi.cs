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
using Strava.XApi.Model;
using System.Collections.Generic;
using NDesk.Options;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Strava.XApi
{
    public class TooManyStravaRequestException : Exception
    {
        public TooManyStravaRequestException(string Message): base(Message)
        {}
    }
    public class ActivityException : Exception
    {
        public string ActivityId {get; protected set;}
        public ActivityException(string AthleteId)
        {
            this.ActivityId=ActivityId;
        }
        public ActivityException(string ActivityId, string message):base(message)
        {
            this.ActivityId=ActivityId;
        }
    }
    public class AthleteException : Exception
    {
        public string AthleteId {get; protected set;}
        public AthleteException(string AthleteId)
        {
            this.AthleteId=AthleteId;
        }
        public AthleteException(string AthleteId, string message):base(message)
        {
            this.AthleteId=AthleteId;
        }
    }
    public class PrivateAthleteException : AthleteException
    {
        public PrivateAthleteException(string AthleteId):base(AthleteId)
        {}
    }
    public class PrivateGpxException : ActivityException
    {
        public PrivateGpxException(string ActivityId):base(ActivityId)
        {}
    }
    public class NoFirstDateFoundException : AthleteException
    {
        public NoFirstDateFoundException(string AthleteId, string message):base(AthleteId, message)
        {}
        
    }
    public class ConnectedAthlete : AthleteShort
    {
        public string ConnectionState { get; set;}
    }
    class StravaXApi: IDisposable
    {
        private ILogger logger;
        private IWebDriver BrowserDriver;
        private Boolean ScreenshotsMonthActivities = false;
        private Boolean DownloadThumbnailsActivities = false;
        private Boolean DownloadImagesActivities = false;
        private Boolean RunBrowserStack = true;
        private string Username;
        private SecureString Password;
        static int Main(string[] args)
        {
            int ret = -1;
            // start command from dotnet run with "dotnet run -- -c=stats"
            // NDesk.Opition:
            // - https://github.com/Latency/NDesk.Options
            // - http://www.ndesk.org/doc/ndesk-options/NDesk.Options/OptionSet.html#T:NDesk.Options.OptionSet:Docs:Example:1
            int verbose = 0;
            var show_help = false;
            string exec_cmd = "stats";
            var p = new OptionSet () {
                { "v|verbose", v => { if (v != null) ++verbose; } },
                { "h|?|help",  "strava-x-api toolkit", v => { show_help = v != null; } },
                { "c|command=",   v => { exec_cmd=v; } },
            };
            Console.WriteLine($"{String.Join(',',args)}");
            p.Parse(args);
            if (show_help)
            {
                p.WriteOptionDescriptions(Console.Out);
                return 0;
            }

            var StravaXApi = new StravaXApi();
            try
            {                
                if(exec_cmd=="stats")
                    ret = Strava.XApi.Tools.DbStats.WriteState(args);
                else if(exec_cmd=="init")
                    ret = Strava.XApi.Tools.DbInit.EnsureCreated(args);
                else if(exec_cmd=="clone")
                    ret = Strava.XApi.Tools.DbClone.DoClone(args);
                else if(exec_cmd=="get-images")
                    ret = Strava.XApi.Tools.ImageDownloader.Downloader(args);
                else if(exec_cmd=="gpx2kml")
                    ret = Strava.XApi.Tools.GpxToKml.Convert(args);
                else
                {
                    switch(exec_cmd)
                    {
                        case "get-activities":
                        case "get-gpx":
                        case "get-athletes":
                        case "get-queries":
                        case "query-activities":
                        break;
                        default:
                            throw new ArgumentException($"command for {exec_cmd} is not defined.");
                    }
                    StravaXApi.initialize(args);
                    switch(exec_cmd)
                    {
                        case "get-activities":
                            ret = Strava.XApi.Tools.ActivitiesCrawler.ReadActivitiesForAthlete(StravaXApi, args);
                        break;
                        case "get-gpx":
                            ret = Strava.XApi.Tools.GpxDownloader.Downloader(StravaXApi, args);
                        break;
                        case "get-athletes":
                            ret = Strava.XApi.Tools.AthletesCrawler.ReadAthleteConnectionsForAthlete(StravaXApi, args);
                        break;
                        case "get-queries":
                            ret = Strava.XApi.Tools.QueriesGenerator.WriteQueriesForAthletes(StravaXApi);
                        break;
                        case "query-activities":
                            ret = Strava.XApi.Tools.QueryActivities.SendQueriesForActivities(StravaXApi, args);
                        break;
                        default:
                            throw new ArgumentException($"command for {exec_cmd} is not defined.");
                    }
                }
            }
            catch(Exception e)
            {
                ret = 2;
                StravaXApi.logger.LogCritical(e.Message);
                StravaXApi.logger.LogCritical(e.StackTrace);
            }
            finally
            {
                StravaXApi.logger.LogDebug("quit strava-x-api tools");
            }
            StravaXApi.logger.LogInformation($"exit StravaXApi tools with {ret}");
            return ret; 
        }
        private void CreateLogger()
        {
            #region snippet_LoggerFactory
            // Logging in .NET Core and ASP.NET Core
            // https://docs.microsoft.com/de-de/aspnet/core/fundamentals/logging/?view=aspnetcore-3.0
            // https://github.com/serilog/serilog/wiki/Getting-Started
            //
            // AspNetCore.Docs
            // https://github.com/aspnet/AspNetCore.Docs/tree/master/aspnetcore/fundamentals/logging/index/samples/3.x/LoggingConsoleApp
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", Microsoft.Extensions.Logging.LogLevel.Warning)
                    .AddFilter("System", Microsoft.Extensions.Logging.LogLevel.Warning)
                    .AddFilter("Strava.XApi.StravaXApi", Microsoft.Extensions.Logging.LogLevel.Information)
                    .AddFilter("StravaXApi", Microsoft.Extensions.Logging.LogLevel.Information)
                    .AddProvider(new Strava.XApi.Tools.CustomLoggerProvider()); // Color and Multi-Line output are not optimal for cloud container logs.
                    //.AddConsole();
                    //.AddEventLog();
            });
            logger = loggerFactory.CreateLogger<StravaXApi>();
            logger.LogDebug("Log strava-x-api");
            #endregion

        }

        StravaXApi()
        {
            CreateLogger();
        }

        void initialize(string[] args)
        {
            String _Username = Environment.GetEnvironmentVariable("STRAVA_USER");
            SecureString _Password = new SecureString();
            foreach(var c in Environment.GetEnvironmentVariable("STRAVA_PWD"))
                _Password.AppendChar(c);
            _Password.MakeReadOnly();
            string SeleniumHub = Environment.GetEnvironmentVariable("SELENIUM_HUB");
            if (Environment.GetEnvironmentVariable("BROWSERSTACK")=="ON")
                initialize_BrowserStack(Environment.GetEnvironmentVariable("BROWSERSTACK_USER"),Environment.GetEnvironmentVariable("BROWSERSTACK_PWD"));
            else if (!string.IsNullOrEmpty(SeleniumHub))
                initialize_SeleniumHub(SeleniumHub);
            else
                initialize_SeleniumLocal();
            
            Username = _Username;
            Password = _Password;
            ScreenshotsMonthActivities = Array.IndexOf(args,"--ScreenshotsMonthActivities") >= 0;
            DownloadThumbnailsActivities = Array.IndexOf(args,"--DownloadThumbnailsActivities") >= 0;
            DownloadImagesActivities = Array.IndexOf(args,"--DownloadImagesActivities") >= 0;
            RunBrowserStack = Array.IndexOf(args,"--RunBrowserStack") >= 0;
        }
        void initialize_SeleniumLocal()
        {
            ChromeOptions Options = new ChromeOptions();
            Options.AddArgument("--window-size=1300,15000");
            Options.AddArgument("--headless");       
            // Options for running chrome-driver in docker. And yes! It was a long way to fix this issue.
            // 
            // https://stackoverflow.com/a/50642913/281188
            Options.AddArgument("--disable-gpu");
            Options.AddArgument("--no-sandbox");            
            Options.AddArgument("--disable-dev-shm-usage");            
            logger.LogInformation($"start selenium local");
            BrowserDriver = new ChromeDriver(Options);
        }
        void initialize_SeleniumHub(string SeleniumHubUri)
        {
            ChromeOptions Options = new ChromeOptions();
            Options.AddArgument("--window-size=1300,15000");
            Options.AddArgument("--headless");            
            Options.AddArgument("--disable-gpu");
            Options.AddArgument("--no-sandbox");
            logger.LogInformation($"start selenium web driver {SeleniumHubUri}");
            BrowserDriver = new RemoteWebDriver(new Uri(SeleniumHubUri),Options);
        }

        void initialize_BrowserStack(string BrowserStackUserName, string BrowserStackAccessKey)
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

            string SeleniumHubUri="http://hub-cloud.browserstack.com/wd/hub/";
            logger.LogInformation($"start selenium browserstack web driver {SeleniumHubUri}");
            BrowserDriver = new RemoteWebDriver(new Uri(SeleniumHubUri), capability);
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
            try
            {
                new WebDriverWait(BrowserDriver, TimeSpan.FromSeconds(30)).Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists((By.XPath("//div[@class='media']"))));
            }
            catch(WebDriverTimeoutException e)
            {   
                try
                {
                    var Text = BrowserDriver.FindElement(By.XPath("/html/body/pre")).Text;
                    if ("Too Many Requests"==Text)
                    {
                        throw new TooManyStravaRequestException("Strava do not accept to much queries in such a short time, try to delay at least 1000ms between two request. This account have to wait several hours.");
                    }
                }
                catch(WebDriverException)
                {
                    // Everything 'OK', Strava is not in await mode.
                }
                logger.LogError($"Can't login in Strava with user {Username}. Please check if the login page appears at {url}.");
                throw e;
            }
        }

        public void signOut()
        {

        }

        private string readAlertMessage()
        {
            IWebElement AlertElt=null;
            string WarningMessage=null;
            try
            {
                // /html/body/div[2]/div[1]
                // search for alert or error messages on the page.
                var AlertElts=BrowserDriver.FindElements(By.XPath(".//div[contains(@class,'alert-message')]"));
                foreach(var ae in AlertElts)
                {
                    logger.LogDebug($"Strava alert-message found: {ae.Text}");
                    WarningMessage = ae.Text;
                    AlertElt=ae;
                }
                if (WarningMessage==null)
                {
                    AlertElts=BrowserDriver.FindElements(By.XPath(".//div[contains(@class,'alert')]"));
                    foreach(var ae in AlertElts)
                    {
                        logger.LogDebug($"Strava alert found: {ae.Text}");
                        WarningMessage = ae.Text;
                        AlertElt=ae;
                    }
                }
                if (WarningMessage==null)
                {
                    AlertElts=BrowserDriver.FindElements(By.XPath(".//div[contains(@class,'error-page')]"));
                    foreach(var ae in AlertElts)
                    {
                        logger.LogDebug($"Strava error found: {ae.Text}");
                        WarningMessage = ae.Text;
                        AlertElt=ae;
                    }
                }
                // AlertElt=BrowserDriver.FindElement(By.XPath("/html/body/div[2]"));
            }
            catch(WebDriverException e)
            {
                // Or may try one more time (cause Timeout), but first check if it's a private profile.
                logger.LogWarning(e.Message);
                logger.LogDebug(e.StackTrace);
                AlertElt=null;
            }
            return WarningMessage;
        }

        public void getActivityGpxSelenium(String ActivityId, String destFilepath)
        {
            String url = $"https://www.strava.com/activities/{ActivityId}/export_gpx";

            //
            // 1: check download directory content
            //
            string DownloadDir=".";
            // Location where the browser store the downloaded files.
            // If not specified, the current working directory will be used.
            if (Environment.GetEnvironmentVariable("DOWNLOAD_DIR")!=null)
            {
                DownloadDir=Environment.GetEnvironmentVariable("DOWNLOAD_DIR");
            }
            DirectoryInfo di = new DirectoryInfo(DownloadDir);
            List<String> filesBefore = new List<String>();
            foreach(FileInfo fi in di.EnumerateFiles())
            {
                if (fi.Name.ToLower().EndsWith(".gpx"))
                {
                    throw new Exception($"download directory countains gpx file {fi.Name}. Please remove all gpx files from download directory.");
                }
                filesBefore.Add(fi.Name);
            }


            //
            // 2: access download page
            //
            logger.LogInformation($"open {url}");
            BrowserDriver.Navigate().GoToUrl(url);            

            //
            // 3: check alert messages
            //
            string WarningMessage=readAlertMessage();

            IEnumerable<FileInfo> filesAfter = di.EnumerateFiles();

            //
            // 4: wait until download ends.
            //
            bool WaitDownload=true;
            // If there is a warningwe do not need to wait as so long.
            int TimerSeconds=(WarningMessage!=null)?2:10;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            while (WaitDownload)
            {
                int detectedFilesCount=0;
                foreach(FileInfo fi in filesAfter)
                {
                    if (filesBefore.Contains(fi.Name))
                    {
                        // no new file
                        logger.LogDebug($"SKIP file: {fi}");
                    }
                    else
                    {
                        if (fi.Name.EndsWith(".crdownload"))
                        {
                            logger.LogDebug($"WAIT file: {fi} {fi.Name}");
                            WaitDownload=true;
                            Thread.Sleep(200);
                            detectedFilesCount++;
                        }
                        else
                        {
                            logger.LogInformation($"FOUND file: {fi} {fi.Name} Rename {fi} to {destFilepath}");                    
                            WaitDownload=false;
                            try{
                                fi.MoveTo(destFilepath);
                            }
                            catch(FileNotFoundException)
                            {
                                logger.LogWarning($"file to move not found {fi.Name}, wait and try again.");
                                Thread.Sleep(1000);
                                fi.MoveTo(destFilepath);
                            }
                            detectedFilesCount++;
                        }
                    }
                }
                if (detectedFilesCount==0)
                {
                    TimeSpan ts = stopWatch.Elapsed;
                    // logger.LogInformation($"detectedFilesCount: {detectedFilesCount} {ts.TotalSeconds} from {filesAfter.Count()}");
                    if (TimerSeconds>0 && ts.TotalSeconds>TimerSeconds)
                    {
                        // recheck message to be sure
                        if (WarningMessage==null)
                            WarningMessage=readAlertMessage();
                        if (WarningMessage!=null)
                        {
                            // if a warning message exit, we could publify it.
                            throw new PrivateGpxException(WarningMessage);
                        }
                        else
                        {
                            throw new Exception($"can't find GPX track file for {ActivityId} at {url}. wait ({ts.TotalSeconds}s)");
                        }
                    }
                }
                filesAfter = di.EnumerateFiles();
            }
            // after download, no gpx files should be present.
            foreach(FileInfo fi in di.EnumerateFiles())
            {
                if (fi.Name.ToLower().EndsWith(".gpx"))
                {
                    throw new Exception($"download directory countains gpx file {fi.Name}. Please remove all gpx files from download directory.");
                }
            }

        }
        /**
         * Find the oldest possible date for an activity
         */
        public DateTime getActivityRange(String AthleteId)
        {
            // open athlete page
            String url = $"https://www.strava.com/athletes/{AthleteId}";

            BrowserDriver.Navigate().GoToUrl(url);            
            logger.LogInformation($"open {url}");

            ICollection<IWebElement> Elts;
            try
            {
                // search for pull down to locate firste activity date.
                Elts=BrowserDriver.FindElements(By.XPath("//div[@id='interval-graph']/div/div/ul/li/a"));
            }
            catch(WebDriverException)
            {
                // Or may try one more time (cause Timeout), but first check if it's a private profile.
                Elts=null;
            }
            if (Elts==null || Elts.Count==0)
            {
                try
                {
                    var Text = BrowserDriver.FindElement(By.XPath("/html/body/pre")).Text;
                    if ("Too Many Requests"==Text)
                    {
                        throw new TooManyStravaRequestException("Strava do not accept to much queries in such a short time, try to delay at least 1000ms between two request. This account have to wait several hours.");
                    }
                }
                catch(WebDriverException)
                {
                    // Everything OK, Strava is not in await mode.
                }
                // we may not have access to none-public profiles.
                // //*[@id="athlete-profile"]/div[2]/div[1]/div[1]/div/div[1]/div[2]/button
                try
                {
                    //*[@id="athlete-profile"]/div[1]/div[2]/div[1]/div[3]/button
                    var Visibility=BrowserDriver.FindElement(By.XPath("//div[@class='follow-action']/button"));
                    if ("Request to Follow"==Visibility.Text || "Folge-Anfrage"==Visibility.Text)
                    {
                        throw new PrivateAthleteException(AthleteId);
                    }
                    else // if ("Follow"==Visibility.Text)
                    {
                        // Athlete is public but has no activities.
                        // Or may try one more time
                        throw new NoFirstDateFoundException(AthleteId, $"no activities found for {AthleteId} visibility: {Visibility.Text}");
                    }
                }
                catch(WebDriverException)
                {
                    // Or may try one more time
                    throw new NoFirstDateFoundException(AthleteId, $"no activities found for {AthleteId} visibility: UNKNOWN");
                }
            }
            
            // locate dates in the pulldown menu
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
            logger.LogDebug($"Begin = {YearBeginInt}/{WeekBeginInt} {FirstActivityDate}");

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
        public List<AthleteShort> getConnectedAthetes(AthleteShort Athlete, string FollowType)
        {
            string NextPageUrl = $"https://www.strava.com/athletes/{Athlete.AthleteId}/follows?type={FollowType}";

            // BrowserDriver.Navigate().GoToUrl(NextPageUrl);            
            List<AthleteShort> AthleteShortList = new List<AthleteShort>();
            DateTime CrawlDate = DateTime.Now;
            int PageCount=0;
            do
            {
                logger.LogInformation($"open {NextPageUrl}");
                BrowserDriver.Navigate().GoToUrl(NextPageUrl);            
                var ConnectedAthleteElts=BrowserDriver.FindElements(By.XPath("//li[@data-athlete-id]"));
                foreach (IWebElement ConnectedAthleteElt in ConnectedAthleteElts)
                {
                    try{
                        var ConnectedAthleteId = ConnectedAthleteElt.GetAttribute("data-athlete-id");  
                        logger.LogDebug($"ConnectedAthleteId {ConnectedAthleteId}");              
                        var ConnectedAthleteName = ConnectedAthleteElt.FindElement(By.XPath("./div[@title]")).GetAttribute("title");
                        logger.LogDebug($"ConnectedAthleteName {ConnectedAthleteName}");              
                        var ConnectedAthleteAvatarUrl = ConnectedAthleteElt.FindElement(By.XPath(".//img[@class='avatar-img']")).GetAttribute("src");
                        logger.LogDebug($"ConnectedAthleteAvatarUrl {ConnectedAthleteAvatarUrl}");              
                        var ConnectedAthleteBadge = ConnectedAthleteElt.FindElement(By.XPath(".//div[@class='avatar-badge']/span/span")).GetAttribute("class");
                        logger.LogDebug($"ConnectedAthleteBadge {ConnectedAthleteBadge}");              
                        var ConnectedAthleteLocation = ConnectedAthleteElt.FindElement(By.XPath(".//div[@class='location mt-0']")).Text;
                        logger.LogDebug($"ConnectedAthleteLocation {ConnectedAthleteLocation}");              
                        var AthleteConnectionType = ConnectedAthleteElt.FindElement(By.XPath(".//button")).Text;
                        logger.LogDebug($"AthleteConnectionType {AthleteConnectionType}");              

                        var AthleteShort = new ConnectedAthlete();
                        AthleteShort.AthleteId = ConnectedAthleteId;
                        AthleteShort.AthleteName = ConnectedAthleteName;
                        AthleteShort.AthleteAvatarUrl = ConnectedAthleteAvatarUrl;
                        AthleteShort.AthleteBadge = ConnectedAthleteBadge;
                        AthleteShort.AthleteLocation = ConnectedAthleteLocation;
                        AthleteShort.AthleteLastCrawled = CrawlDate;
                        AthleteShortList.Add(AthleteShort);
                        logger.LogInformation($"add {AthleteShort}");              
                        // We also have informations about the connection state
                        AthleteShort.ConnectionState = AthleteConnectionType;
                    }
                    catch (Exception e) when (e is WebDriverException || e is NotFoundException)
                    {
                        if (e is InvalidElementStateException || e is StaleElementReferenceException)
                        {
                            // Page seams to be incorrect loaded. Probably need to wait more.
                            throw e;
                        }
                        logger.LogInformation($"Skip athlete at {NextPageUrl} Err:{e.Message}");
                    }
                }
                try
                {
                    NextPageUrl = BrowserDriver.FindElement(By.XPath("//li[@class='next_page']/a")).GetAttribute("href");
                    logger.LogDebug($"next page={NextPageUrl}");
                }
                catch(WebDriverException)
                {
                    NextPageUrl = "";
                }
                PageCount++;
            }
            while(!string.IsNullOrEmpty(NextPageUrl));
            return AthleteShortList;
        }

        public List<ActivityShort> getActivities(String AthleteId, String Year, String Month)
        {
            try{
                return _getActivities(AthleteId, Year, Month);
            }
            catch (Exception e) when (e is WebDriverException || e is NotFoundException)
            {
                Thread.Sleep(5000);
                return _getActivities(AthleteId, Year, Month);
            }
        }
        private List<ActivityShort> _getActivities(String AthleteId, String Year, String Month)
        {
            String url = $"https://www.strava.com/athletes/{AthleteId}#interval_type?chart_type=miles&interval_type=month&interval={Year}{Month}&year_offset=0";

            BrowserDriver.Navigate().GoToUrl(url);            
            logger.LogInformation($"open {url}");
            DateTime CrawlDate = DateTime.Now;
            // Should wait for element.
            // Thread.Sleep(2000);
            IWait<IWebDriver> wait = new OpenQA.Selenium.Support.UI.WebDriverWait(BrowserDriver, TimeSpan.FromSeconds(30.00));
            wait.Until(driver1 => ((IJavaScriptExecutor)BrowserDriver).ExecuteScript("return document.readyState").Equals("complete"));
            Thread.Sleep(3000);

            if (ScreenshotsMonthActivities)
            {
                if (!Directory.Exists("./screenshots"))
                {
                    DirectoryInfo DirInfo = Directory.CreateDirectory("./screenshots");
                    logger.LogInformation($"directory for screenshots created at {DirInfo.FullName}");
                }
                if (!Directory.Exists($"./screenshots/{AthleteId}"))
                {
                    DirectoryInfo DirInfo = Directory.CreateDirectory($"./screenshots/{AthleteId}");
                }
                ((ITakesScreenshot)BrowserDriver).GetScreenshot().SaveAsFile($"./screenshots/{AthleteId}/{AthleteId}_{Year}_{Month}.png");
            }

            // Find all activity icons in thos page
            var Elts=BrowserDriver.FindElements(By.XPath("//div[@class='entry-type-icon']"));
            logger.LogDebug($"Elts count={Elts.Count} for {Year}/{Month}");

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
                        logger.LogDebug($"Activity {ActivityId} url {imageUrl}");
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
                        logger.LogDebug($"Activity {ActivityId} url {imageUrl}");
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
                            logger.LogDebug($"Groupped activity : Activity {ActivityId} Athlete {AthleteIdInGroup}");

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
                                    logger.LogDebug($"Group {ActivityId} with {GroupActivityString} from {GroupAthleteId}");
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

                    // get stats short description as text
                    string ActivityStatShort;
                    IWebElement StatShortElt;
                    StatShortElt = ActivityNumberElt.FindElement(By.XPath(".//ul[contains(@class,'list-stats')]"));
                    ActivityStatShort=StatShortElt.Text;

                    // Retrieve the activity class, with that it's poosible to know the activity type
                    var ActivityTypeElt = Elt.FindElement(By.XPath("./span/span"));
                    ActivityType ActivityType = parseActivityType(ActivityTypeElt.GetAttribute("class"));

                    DateTime ActivityTime = DateTime.Parse(ActivityTimeString.Substring(0,ActivityTimeString.Length-4));
                    logger.LogInformation($"Id={ActivityId} Text={ActivityTitle} Type={ActivityType} Time={ActivityTime}");                    
                    var ActivityShort = new ActivityShort();
                    ActivityShort.AthleteId = AthleteId;
                    ActivityShort.ActivityId = ActivityId;
                    ActivityShort.ActivityTitle = ActivityTitle;
                    ActivityShort.ActivityType = ActivityType;
                    ActivityShort.StatShortString = ActivityStatShort;
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
                    logger.LogInformation($"Skip Activity at {url} Err:{e.Message}");
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
                logger.LogInformation("{0} is not an underlying value of the ActivityType enumeration.", ActivityTypeString);
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

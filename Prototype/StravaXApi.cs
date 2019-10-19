using System;
using System.Threading;
using System.IO;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Security;
using Prototype.Model;
using System.Collections.Generic;
namespace Prototype
{
    class StravaXApi: IDisposable
    {
        private IWebDriver BrowserDriver;
        private static string AthleteId = "144100";
        static void Main(string[] args)
        {
            Console.WriteLine("Call API");

            Console.WriteLine("Enter username: ");
            String Username = args[0];
            SecureString Password = new SecureString();
            foreach(var c in args[1])
                Password.AppendChar(c);
            Password.MakeReadOnly();

            StravaXApi stravaXApi = new StravaXApi();
            try
            {
                stravaXApi.signIn(Username,Password);
                List<ActivityShort> ActivitiesList = new List<ActivityShort>();

                for(int year=2019;year<=2019;year++)
                {
                    for(int month=1;month<=10;month++)
                    {
                        List<ActivityShort> ActivitiesMonthList;
                        try
                        {
                            ActivitiesMonthList = stravaXApi.getActivities(AthleteId,$"{year:D4}",$"{month:D2}");
                        }
                        catch(StaleElementReferenceException)
                        {
                            // Wait and try again.
                            ActivitiesMonthList = stravaXApi.getActivities(AthleteId,$"{year:D4}",$"{month:D2}");
                        }
                        ActivitiesList.AddRange(ActivitiesMonthList);
                    }
                }

                foreach(ActivityShort ActivityShort in ActivitiesList)
                {
                    Console.WriteLine($"Activity={ActivityShort}");
                }
                Console.WriteLine($"Activities ={ActivitiesList.Count}");
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
        public void signIn(String Username, SecureString Password)
        {
            String url = $"https://www.strava.com/login";
            BrowserDriver.Navigate().GoToUrl(url);
            // Enter login data
            BrowserDriver.FindElement(By.Name("email")).SendKeys(Username);
            BrowserDriver.FindElement(By.Name("password")).SendKeys(new System.Net.NetworkCredential("", Password).Password);
            BrowserDriver.FindElement(By.Id("login-button")).Click();
            // Wait until Login is done.
            new WebDriverWait(BrowserDriver, TimeSpan.FromSeconds(30)).Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists((By.XPath("//span[@class='app-icon-wrapper']"))));
        }
        public List<ActivityShort> getActivities(String AthleteId, String Year, String Month)
        {
            String url = $"https://www.strava.com/athletes/{AthleteId}#interval_type?chart_type=miles&interval_type=month&interval={Year}{Month}&year_offset=0";

            BrowserDriver.Navigate().GoToUrl(url);
            Console.WriteLine($"open ${url}");
            // Should wait for element.
            Thread.Sleep(2000);

            if (!Directory.Exists("./screenshots"))
            {
                DirectoryInfo DirInfo = Directory.CreateDirectory("./screenshots");
                Console.WriteLine($"directory for screenshots created at {DirInfo.FullName}");
            }
            ((ITakesScreenshot)BrowserDriver).GetScreenshot().SaveAsFile($"./screenshots/{AthleteId}_{Year}_{Month}.png");

            // Find all activity icons in thos page
            var Elts=BrowserDriver.FindElements(By.XPath("//div[@class='entry-type-icon']"));
            Console.WriteLine($"Elts count={Elts.Count} for {Year}/{Month}");

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

                    // Locate activity time information
                    string ActivityTimeString = "";
                    IWebElement ActivityTimeElt;
                    try{
                        if (ActivityNumberElt.TagName == "li")
                        {
                            // because of group activities I need to go to parents higher.
                            ActivityTimeElt = ActivityNumberElt.FindElement(By.XPath("./../..//time[@class='timestamp']"));
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

                    // Retrieve the activity class, with that it's poosible to know the activity type
                    var ActivityTypeElt = Elt.FindElement(By.XPath("./span/span"));
                    ActivityType ActivityType = parseActivityType(ActivityTypeElt.GetAttribute("class"));

                    DateTime ActivityTime = DateTime.Parse(ActivityTimeString.Substring(0,ActivityTimeString.Length-4));
                    Console.WriteLine($"Id={ActivityId} Type={ActivityType} Time={ActivityTime}");
                    var ActivityShort = new ActivityShort(ActivityId.Substring("Activity-".Length),ActivityType,ActivityTime);
                    ActivitiesList.Add(ActivityShort);
                }
                catch (Exception e) when (e is WebDriverException || e is NotFoundException)
                {
                    if (e is InvalidElementStateException)
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

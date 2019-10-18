using System;
using System.Threading;
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
        public static IWebDriver BrowserDriver;
        static void Main(string[] args)
        {
            Console.WriteLine("Call API");

            Console.WriteLine("Enter username: ");
            String Username = args[0];
            SecureString Password = new SecureString();
            foreach(var c in args[1])
                Password.AppendChar(c);
            Password.MakeReadOnly();

            ChromeOptions Options = new ChromeOptions();
            Options.AddArgument("--window-size=1920,4000");
            BrowserDriver = new ChromeDriver(Options);
            StravaXApi stravaXApi = new StravaXApi();
            try
            {
                stravaXApi.signIn(Username,Password);
                List<ActivityShort> ActivitiesList = new List<ActivityShort>();

                List<ActivityShort> ActivitiesMonthList = stravaXApi.getActivities("144100","2019","10");
                ActivitiesList.AddRange(ActivitiesMonthList);
                ActivitiesMonthList = stravaXApi.getActivities("144100","2019","09");
                ActivitiesList.AddRange(ActivitiesMonthList);
                ActivitiesMonthList = stravaXApi.getActivities("144100","2019","08");
                ActivitiesList.AddRange(ActivitiesMonthList);
                ActivitiesMonthList = stravaXApi.getActivities("144100","2019","07");
                ActivitiesList.AddRange(ActivitiesMonthList);
                ActivitiesMonthList = stravaXApi.getActivities("144100","2019","06");
                ActivitiesList.AddRange(ActivitiesMonthList);
                ActivitiesMonthList = stravaXApi.getActivities("144100","2019","05");
                ActivitiesList.AddRange(ActivitiesMonthList);
                ActivitiesMonthList = stravaXApi.getActivities("144100","2019","04");
                ActivitiesList.AddRange(ActivitiesMonthList);

                foreach(ActivityShort ActivityShort in ActivitiesList)
                {
                    System.Console.WriteLine($"Activity={ActivityShort}");
                }
                System.Console.WriteLine($"Activities ={ActivitiesList.Count}");
            }
            catch(Exception e)
            {
                Console.WriteLine($"ERROR:{e.Message}");                
            }
            finally
            {
                stravaXApi.Dispose();
            }
        }
        public void signIn(String Username, SecureString Password)
        {
            String url = $"https://www.strava.com/login";
            BrowserDriver.Navigate().GoToUrl(url);
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
            // Should wait for element.
            Thread.Sleep(4000);

            // Find all activity icons
            var Elts=BrowserDriver.FindElements(By.XPath("//div[@class='entry-type-icon']"));
            System.Console.WriteLine($"Elts count={Elts.Count} for {Year}/{Month}");

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
                    string ActivityTime = "";
                    try{
                        var ActivityTimeElt = ActivityNumberElt.FindElement(By.XPath(".//time[@class='timestamp']"));
                        ActivityTime = ActivityTimeElt.GetAttribute("datetime");
                    }
                    catch(OpenQA.Selenium.WebDriverException e) {
                        System.Console.WriteLine($"Skip Activity @{url} Err:{e.Message}");
                    }

                    // Retrieve the activity class, with that it's poosible to know the activity type
                    var ActivityTypeElt = Elt.FindElement(By.XPath("./span/span"));
                    ActivityType ActivityType = parseActivityType(ActivityTypeElt.GetAttribute("class"));
                    System.Console.WriteLine($"Id={ActivityId} Type={ActivityType} Time={ActivityTime}");
                    var ActivityShort = new ActivityShort(ActivityId.Substring("Activity-".Length),ActivityType,ActivityTime);
                    ActivitiesList.Add(ActivityShort);
                }
                catch(OpenQA.Selenium.WebDriverException e) {
                    System.Console.WriteLine($"Skip Activity @{url} Err:{e.Message}");
                }
            }
            return ActivitiesList;
        }
        private ActivityType parseActivityType(string ActivityTypeCssClass)
        {
            if (ActivityTypeCssClass.Contains("icon-swim"))
                return ActivityType.Swim;
            else if (ActivityTypeCssClass.Contains("icon-run"))
                return ActivityType.Run;
            else if (ActivityTypeCssClass.Contains("icon-virtualride"))
                return ActivityType.VirtualRide;
            else if (ActivityTypeCssClass.Contains("icon-backcountryski"))
                return ActivityType.BackcountrySki;
            else if (ActivityTypeCssClass.Contains("icon-alpineski"))
                return ActivityType.AlpineSki;
            else if (ActivityTypeCssClass.Contains("icon-crosscountryskiing"))
                return ActivityType.CrossCountrySkiing;
            else if (ActivityTypeCssClass.Contains("icon-nordicski"))
                return ActivityType.NordicSki;
            else if (ActivityTypeCssClass.Contains("icon-snowboard"))
                return ActivityType.Snowboard;
            return ActivityType.Workout;
        }
        public void Dispose()
        {
            // Close Browser.
            BrowserDriver.Quit();
        }
    }
}

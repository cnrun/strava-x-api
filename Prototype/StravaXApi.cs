using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using SeleniumExtras.WaitHelpers;
using OpenQA.Selenium.Support.UI;
using System.Security;

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

            BrowserDriver = new ChromeDriver();
            StravaXApi stravaXApi = new StravaXApi();
            try
            {
                stravaXApi.signIn(Username,Password);
                stravaXApi.getActivities("144100","2019","10");
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
        public String[] getActivities(String AthleteId, String Year, String Month)
        {
            String url = $"https://www.strava.com/athletes/{AthleteId}#interval_type?chart_type=miles&interval_type=month&interval={Year}{Month}&year_offset=0";

            BrowserDriver.Navigate().GoToUrl(url);
            // Should wait for element.
            Thread.Sleep(2000);

            // Find all activity icons
            var Elts=BrowserDriver.FindElements(By.XPath("//div[@class='entry-type-icon']"));
            Console.WriteLine($"Elts count={Elts.Count} for {Year}/{Month}");
            foreach (IWebElement Elt in Elts)
            {
                // locate the div for activity number
                var ActivityNumberElt = Elt.FindElement(By.XPath("./../../.."));
                var ActivityId=ActivityNumberElt.GetAttribute("id");
                // for activitty with picture we have to search on step higher
                if (String.IsNullOrEmpty(ActivityId)) {
                    ActivityNumberElt = ActivityNumberElt.FindElement(By.XPath("./.."));
                    ActivityId=ActivityNumberElt.GetAttribute("id");
                }
                // Retrieve the activity class, with that it's poosible to know the activity type
                var ActivityTypeElt = Elt.FindElement(By.XPath("./span/span"));
                System.Console.WriteLine($"Id={ActivityId} Class={ActivityTypeElt.GetAttribute("class")}");
            }
            // Dummy return, everything in stdout
            return new String[0];
        }
        public void Dispose()
        {
            // Close Browser.
            BrowserDriver.Quit();
        }
    }
}

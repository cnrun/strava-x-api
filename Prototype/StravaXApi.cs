using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
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

        }
        public String[] getActivities(String AthleteId, String Year, String Month)
        {
            String url = $"https://www.strava.com/athletes/{AthleteId}#interval_type?interval_type=month&interval={Year}{Month}&year_offset=0";
            BrowserDriver.Navigate().GoToUrl(url);
            var Elts=BrowserDriver.FindElements(By.XPath("//div[@class='activity entity-details feed-entry']"));
            Console.WriteLine($"Elts count={Elts.Count}");
            foreach (IWebElement Elt in Elts)
            {
                System.Console.WriteLine($"ActivityId={Elt.GetAttribute("id")}");
            }
            return new String[0];
        }
        public void Dispose()
        {
           BrowserDriver.Quit();
        }
    }
}

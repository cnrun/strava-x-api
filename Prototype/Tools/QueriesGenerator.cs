using System;
using System.Threading;
using System.Linq;
using OpenQA.Selenium;
using Prototype.Model;
using System.Collections.Generic;

namespace Prototype.Tools
{    
    public class QueriesGenerator
    {
        static public void WriteQueriesForAthlete(string[] args)
        {
            Console.WriteLine("Create range queries.");
            if (args.Length < 1)
            {
                Console.WriteLine("Please find the needed arguments from the code 😛. Oh there are several options with environment variables! ");
                return;
            }

            String AthleteId = args[0];
            StravaXApi stravaXApi = StravaXApi.GetStravaXApi(args);

            try
            {
                stravaXApi.signIn();
                DateTime FirstActivityDate = stravaXApi.getActivityRange(AthleteId);
                System.Console.WriteLine($"First activity at {FirstActivityDate.Year}/{FirstActivityDate.Month}");                    
                DateTime Now = DateTime.Now;

                int FromYear=FirstActivityDate.Year;
                int FromMonth=FirstActivityDate.Month;
                int ToYear=Now.Year;
                int ToMonth=Now.Month;
                using (StravaXApiContext db = new StravaXApiContext())
                {
                    Console.WriteLine($"Enterred queries: {db.ActivityQueriesDB.Count()}");
                    // first year
                    for(int month=FromMonth;month<=12;month++)
                    {
                        ActivityRangeQuery query = new ActivityRangeQuery();
                        query.AthleteId=AthleteId;
                        query.DateFrom=new DateTime(FromYear,month,1);
                        query.DateTo=new DateTime(FromYear,month,1).AddMonths(1).AddDays(-1); // last day of the month
                        db.ActivityQueriesDB.Add(query);
                        db.SaveChanges();

                        Console.WriteLine($"query for {month}/{FromYear}");
                    }
                    // all years after
                    for(int year=FromYear+1;year<=ToYear-1;year++)
                    {
                        for(int month=01;month<=12;month++)
                        {
                            ActivityRangeQuery query = new ActivityRangeQuery();
                            query.AthleteId=AthleteId;
                            query.DateFrom=new DateTime(FromYear,month,1);
                            query.DateTo=new DateTime(FromYear,month,1).AddMonths(1).AddDays(-1); // last day of the month
                            db.ActivityQueriesDB.Add(query);
                            db.SaveChanges();
                            Console.WriteLine($"query for {month}/{year}");
                        }
                    }
                    // last year
                    for(int month=01;month<=ToMonth;month++)
                    {
                        ActivityRangeQuery query = new ActivityRangeQuery();
                        query.AthleteId=AthleteId;
                        query.DateFrom=new DateTime(FromYear,month,1);
                        query.DateTo=new DateTime(FromYear,month,1).AddMonths(1).AddDays(-1); // last day of the month
                        db.ActivityQueriesDB.Add(query);
                        db.SaveChanges();
                        Console.WriteLine($"query for {month}/{ToYear}");
                    }
                    Console.WriteLine($"Enterred queries: {db.ActivityQueriesDB.Count()}");
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
    }
}

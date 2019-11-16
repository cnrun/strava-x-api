using System;
using System.Threading;
using System.Linq;
using OpenQA.Selenium;
using Prototype.Model;
using Prototype;
using System.Collections.Generic;

namespace Prototype.Tools
{    
    public class AthletesCrawler
    {
        static internal void ReadAthleteConnectionsForAthlete(StravaXApi stravaXApi, string[] args)
        {
            Console.WriteLine("Read athlete connections with Strava-X-API.");
            if (args.Length < 1)
            {
                Console.WriteLine("Please find the needed arguments from the code 😛. Oh there are several options with environment variables! ");
                return;
            }

            String AthleteId = args[0];

            try
            {
                stravaXApi.signIn();
                AthleteShort AthleteMasterShort;
                using (StravaXApiContext db = new StravaXApiContext())
                {
                    AthleteMasterShort = db.AthleteShortDB.Find(AthleteId);
                    if (AthleteMasterShort==null)
                    {
                        AthleteMasterShort = new AthleteShort();
                        // create a dummy master
                        AthleteMasterShort.AthleteId = AthleteId;
                        // [TODO] other parameters should be retrieved with selenium
                        AthleteMasterShort = db.AthleteShortDB.Add(AthleteMasterShort).Entity;
                        db.SaveChanges();
                    }
                    else
                    {
                        // Eagerly Loading prevent the list to be loaded at creation
                        // https://docs.microsoft.com/de-de/ef/ef6/querying/related-data
                        db.Entry(AthleteMasterShort).Collection(p => p.Connections).Load();

                        Console.WriteLine($"Athlete {AthleteMasterShort.AthleteId} allready enterred with {AthleteMasterShort.Connections.Count} connections {string.Join(',',AthleteMasterShort.Connections)}");
                    }

                    string FollowType="following";
                    var AthleteShortList = stravaXApi.getConnectedAthetes(AthleteMasterShort,FollowType);
                    Console.WriteLine($"Athlete {AthleteId} has {AthleteShortList.Count} connections");

                    foreach(AthleteShort _AthleteShort in AthleteShortList)
                    {
                        AthleteShort AthleteShortfromDb;
                        // Console.WriteLine($"JSON={ActivityShort.SerializePrettyPrint(ActivityShort)}");
                        AthleteShortfromDb = db.AthleteShortDB.Find(_AthleteShort.AthleteId);
                        if (AthleteShortfromDb==null)
                        {
                            // add athlete to the db if need.
                            AthleteShortfromDb = db.AthleteShortDB.Add(_AthleteShort).Entity;
                        }
                        else
                        {
                            Console.WriteLine($"{AthleteShortfromDb.AthleteId} allready in database");
                        }
                        Console.WriteLine($"Enterred Activities: {db.AthleteShortDB.OrderBy(b => b.AthleteId).Count()}");
                        // such the connected athlete with they id.
                        AthleteConnection _ConnectedAthleteShort = AthleteMasterShort.Connections.FirstOrDefault(a=>a.ToId.Equals(_AthleteShort.AthleteId));
                        if (_ConnectedAthleteShort==null)
                        {
                            // add connection if needed.
                            AthleteConnection ac = new AthleteConnection();
                            ac.FromId=AthleteMasterShort.AthleteId;
                            ac.ToId=AthleteShortfromDb.AthleteId;
                            ac.Type=FollowType;
                            ac.ConnectionState=((ConnectedAthlete)_AthleteShort).ConnectionState;

                            AthleteMasterShort.Connections.Add(ac);
                            Console.WriteLine($"athlete {AthleteMasterShort.AthleteId} has {AthleteMasterShort.Connections.Count} connection(s). Added: {_AthleteShort.AthleteId}");
                        }
                        else
                        {
                            Console.WriteLine($"athlete {AthleteMasterShort.AthleteId} already connected to {_AthleteShort.AthleteId} with {AthleteMasterShort.Connections.Count} connection(s)");
                        }
                    }
                    db.SaveChanges();
                    Console.WriteLine($"total read = {AthleteShortList.Count}");
                    Console.WriteLine($"total stored = {db.AthleteShortDB.OrderBy(b => b.AthleteId).Count()}");
                    AthleteShortList.Clear();
                }
                using (StravaXApiContext db = new StravaXApiContext())
                {
                    AthleteMasterShort = db.AthleteShortDB.Find(AthleteId);
                    if (AthleteMasterShort==null)
                    {
                        AthleteMasterShort = new AthleteShort();
                        // create a dummy master
                        AthleteMasterShort.AthleteId = AthleteId;
                        // [TODO] other parameters should be retrieved with selenium
                        AthleteMasterShort = db.AthleteShortDB.Add(AthleteMasterShort).Entity;
                        db.SaveChanges();
                    }
                    else
                    {
                        // Eagerly Loading prevent the list to be loaded at creation
                        // https://docs.microsoft.com/de-de/ef/ef6/querying/related-data
                        db.Entry(AthleteMasterShort).Collection(p => p.Connections).Load();

                        Console.WriteLine($"Athlete {AthleteMasterShort.AthleteId} allready enterred with {AthleteMasterShort.Connections.Count} connections {string.Join(',',AthleteMasterShort.Connections)}");
                    }

                    string FollowType="followers";
                    var AthleteShortList = stravaXApi.getConnectedAthetes(AthleteMasterShort,FollowType);
                    Console.WriteLine($"Athlete {AthleteId} has {AthleteShortList.Count} connections");

                    foreach(AthleteShort _AthleteShort in AthleteShortList)
                    {
                        AthleteShort AthleteShortfromDb;
                        // Console.WriteLine($"JSON={ActivityShort.SerializePrettyPrint(ActivityShort)}");
                        AthleteShortfromDb = db.AthleteShortDB.Find(_AthleteShort.AthleteId);
                        if (AthleteShortfromDb==null)
                        {
                            // add athlete to the db if need.
                            AthleteShortfromDb = db.AthleteShortDB.Add(_AthleteShort).Entity;
                        }
                        else
                        {
                            Console.WriteLine($"{AthleteShortfromDb.AthleteId} allready in database");
                        }
                        Console.WriteLine($"Enterred Activities: {db.AthleteShortDB.OrderBy(b => b.AthleteId).Count()}");
                        // such the connected athlete with they id.
                        AthleteConnection _ConnectedAthleteShort = AthleteMasterShort.Connections.FirstOrDefault(a=>a.ToId.Equals(_AthleteShort.AthleteId));
                        if (_ConnectedAthleteShort==null)
                        {
                            // add connection if needed.
                            AthleteConnection ac = new AthleteConnection();
                            ac.FromId=AthleteMasterShort.AthleteId;
                            ac.ToId=AthleteShortfromDb.AthleteId;
                            ac.Type=FollowType;

                            AthleteMasterShort.Connections.Add(ac);
                            Console.WriteLine($"athlete {AthleteMasterShort.AthleteId} has {AthleteMasterShort.Connections.Count} connection(s). Added: {_AthleteShort.AthleteId}");
                        }
                        else
                        {
                            Console.WriteLine($"athlete {AthleteMasterShort.AthleteId} already connected to {_AthleteShort.AthleteId} with {AthleteMasterShort.Connections.Count} connection(s)");
                        }
                    }
                    db.SaveChanges();
                    Console.WriteLine($"total read = {AthleteShortList.Count}");
                    Console.WriteLine($"total stored = {db.AthleteShortDB.OrderBy(b => b.AthleteId).Count()}");
                    AthleteShortList.Clear();
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

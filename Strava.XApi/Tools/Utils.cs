using System;
using Strava.XApi.Model;
using System.Text.RegularExpressions;

namespace Strava.XApi.Tools
{    
    public class TimeSpanNotFoundException : Exception
    {
        string activityDescription;
        public TimeSpanNotFoundException(string activityDescription):base($"can't extract time span from activity description '{activityDescription}'")
        {
            this.activityDescription=activityDescription;
        }
    }

    public class Utils
    {
        // Retrieve the time of the activity. The value may be container in the short description.
        // If this information is not availlable, an exception wil be thrown.
        public static TimeSpan extractActivityTime(ActivityShort activity)
        {
            string description = activity.StatShortString;
            string hString="0";
            string mString="0";
            string sString="0";
            var match = Regex.Match(description,"([0-9]{1,2})h ([0-9]{1,2})m ([0-9]{1,2})s");
            if (match.Success)
            {
                hString=match.Groups[1].Value;
                mString=match.Groups[2].Value;
                sString=match.Groups[3].Value;
            }
            else
            {
                match = Regex.Match(description,"([0-9]{1,2})h ([0-9]{1,2})m");
                if (match.Success)
                {
                    hString=match.Groups[1].Value;
                    mString=match.Groups[2].Value;
                }
                else
                {
                    match = Regex.Match(description,"([0-9]{1,2})m ([0-9]{1,2})s");
                    if (match.Success)
                    {
                        mString=match.Groups[1].Value;
                        sString=match.Groups[2].Value;
                    }
                    else
                    {
                        match = Regex.Match(description,"([0-9]{1,2})s");
                        if (match.Success)
                        {
                            sString=match.Groups[1].Value;
                        }
                        else
                        {
                            // no time information availlable
                            throw new TimeSpanNotFoundException(description);
                        }
                    }                    
                }
            }
            TimeSpan ret = new TimeSpan(int.Parse(hString),int.Parse(mString),int.Parse(sString));
            return ret;
        }
    }
}

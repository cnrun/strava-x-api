using System;

namespace Prototype.Model
{    
    class ActivityShort
    {
        ActivityType ActivityType;
        DateTime ActivityDate;
        string ActivityId;

        public ActivityShort(string ActivityId, ActivityType ActivityType, DateTime ActivityDate)
        {
            this.ActivityId = ActivityId;
            this.ActivityType = ActivityType;
            this.ActivityDate = ActivityDate;
        }
        override public string ToString()
        {
            return $"{ActivityId} {ActivityType} {ActivityDate}";
        }
    }
}

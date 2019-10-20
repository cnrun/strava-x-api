using System;

namespace Prototype.Model
{    
    class ActivityShort
    {
        ActivityType ActivityType;
        DateTime ActivityDate;
        string ActivityId;
        string AthleteId;

        public ActivityShort(string AthleteId, string ActivityId, ActivityType ActivityType, DateTime ActivityDate)
        {
            this.AthleteId = AthleteId;
            this.ActivityId = ActivityId;
            this.ActivityType = ActivityType;
            this.ActivityDate = ActivityDate;
        }
        override public string ToString()
        {
            return $"athlete:{AthleteId} activity:{ActivityId} type:{ActivityType} date:{ActivityDate}";
        }
    }
}

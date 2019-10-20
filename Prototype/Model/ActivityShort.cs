using System;

namespace Prototype.Model
{    
    class ActivityShort
    {
        ActivityType ActivityType;
        DateTime ActivityDate;
        string ActivityId;
        string ActivityTitle;
        string AthleteId;

        public ActivityShort(string AthleteId, string ActivityId, string ActivityTitle, ActivityType ActivityType, DateTime ActivityDate)
        {
            this.AthleteId = AthleteId;
            this.ActivityId = ActivityId;
            this.ActivityTitle = ActivityTitle;

            this.ActivityType = ActivityType;
            this.ActivityDate = ActivityDate;
        }
        override public string ToString()
        {
            return $"athlete:{AthleteId} activity:{ActivityId} text:'{ActivityTitle}' type:{ActivityType} date:{ActivityDate}";
        }
    }
}

namespace Prototype.Model
{    
    class ActivityShort
    {
        ActivityType ActivityType;
        string ActivityDate;
        string ActivityId;

        public ActivityShort(string ActivityId, ActivityType ActivityType, string ActivityDate)
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

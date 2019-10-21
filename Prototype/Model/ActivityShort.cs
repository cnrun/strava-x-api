using System;
using System.Collections.Generic;

namespace Prototype.Model
{    
    class ActivityShort
    {
        ActivityType ActivityType;
        DateTime ActivityDate;
        string ActivityId;
        string ActivityTitle;
        string AthleteId;

        List<String> ActivityImagesList;
        List<String> ActivityThumbnailsList;

        public ActivityShort(
            string AthleteId,
            string ActivityId,
            string ActivityTitle,
            ActivityType ActivityType,
            DateTime ActivityDate,
            List<String> ActivityThumbnailsList,
            List<String> ActivityImagesList)
        {
            this.AthleteId = AthleteId;
            this.ActivityId = ActivityId;
            this.ActivityTitle = ActivityTitle;

            this.ActivityType = ActivityType;
            this.ActivityDate = ActivityDate;

            this.ActivityThumbnailsList = ActivityThumbnailsList;
            this.ActivityImagesList = ActivityImagesList;
        }
        override public string ToString()
        {
            return $"athlete:{AthleteId} activity:{ActivityId} text:'{ActivityTitle}' type:{ActivityType} date:{ActivityDate} Images {ActivityThumbnailsList.Count}/{ActivityImagesList.Count}";
        }
    }
}

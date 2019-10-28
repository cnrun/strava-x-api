using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Prototype.Model
{    
    class ActivityShort
    {
        public ActivityType ActivityType { get; set; }
        public DateTime ActivityDate { get; set; }
        public string ActivityId { get; set; }
        public string ActivityTitle { get; set; }
        public string ActivityImageMapUrl { get; set; }
        public string AthleteId { get; set; }

        public List<String> ActivityImagesList { get; set; }
        public List<String> ActivityThumbnailsList { get; set; }
        public List<String> GroupActivityList { get; set; }
        public List<String> GroupAthleteList { get; set; }

        public ActivityShort()
        {
        }
        override public string ToString()
        {
            return $"athlete:{AthleteId} activity:{ActivityId} text:'{ActivityTitle}' type:{ActivityType} date:{ActivityDate} Map {ActivityImageMapUrl} Images {ActivityThumbnailsList.Count}/{ActivityImagesList.Count} Group {GroupActivityList.Count}/{GroupAthleteList.Count}";
        }
        public string Serialize(ActivityShort value)
        {
            return JsonSerializer.Serialize<ActivityShort>(value);
        }
        public string SerializePrettyPrint(ActivityShort value)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            return JsonSerializer.Serialize<ActivityShort>(value, options);
        }
    }
}

using System;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using Strava.XApi.Model;
using System.ComponentModel.DataAnnotations.Schema;


namespace Strava.XApi.Model
{    
    public class ActivityRangeQuery
    {
        // See: https://docs.microsoft.com/en-us/ef/core/modeling/keys
        // Key order has been defined in StravaXApiContext.
        [Key]
        public string AthleteId { get; set; }
        [Key]
        public DateTime DateFrom { get; set; }
        [Key]
        public DateTime DateTo { get; set; }
        // [NotMapped]
        public QueryStatus Status { get; set; }
        // [NotMapped]
        public string RunOn { get; set; }
        // [NotMapped]
        public DateTime StatusChanged { get; set; }
        // [NotMapped]
        public string Message { get; set; }
        public ActivityRangeQuery()
        {

        }
        override public string ToString()
        {
            return $"query activities for:{AthleteId} in [{DateFrom}-{DateTo}] status {Status}";
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

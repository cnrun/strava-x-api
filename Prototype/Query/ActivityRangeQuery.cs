using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Prototype.Model
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
        public ActivityRangeQuery()
        {

        }
        override public string ToString()
        {
            return $"query activities for:{AthleteId} in [{DateFrom}-{DateTo}]";
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

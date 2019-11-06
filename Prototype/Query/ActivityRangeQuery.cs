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
        public Guid Id { get; set; }
        public string AthleteId { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public ActivityRangeQuery()
        {

        }
        override public string ToString()
        {
            return $"query activities for:{AthleteId} in[{DateFrom}-{DateTo}]";
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

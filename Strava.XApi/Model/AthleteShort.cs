using System;
using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Strava.XApi.Model
{    
    public class AthleteShort
    {
        public DateTime AthleteLastCrawled { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string AthleteId { get; set; }
        public string AthleteName { get; set; }
        public string AthleteAvatarUrl { get; set; }
        public string AthleteBadge { get; set; }
        public string AthleteLocation { get; set; }

        public ICollection<AthleteConnection> Connections { get; set; }

        public AthleteShort()
        {
            Connections=new List<AthleteConnection>();
        }
        override public string ToString()
        {
            return $"Id:{AthleteId} name:{AthleteName}";
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

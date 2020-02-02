using System;
using System.Text.Json;

namespace Strava.XApi.Model
{    
    public class AthleteConnection
    {
        // Was a long search, do not really understand why just works with this Id
        // Without any anotations. I should be missing something in ET6 specs...
        // https://stackoverflow.com/a/55675811/281188
        public Guid Id { get; set; }

        // 'Following' or 'Followers'?
        public String Type { get; set; }
        // 'Request to Follow' or 'Follow'?
        public String ConnectionState { get; set; }
        // who is connected
        public string FromId { get; set; }
        // with whom is he connected
        public string ToId { get; set; }

        override public string ToString()
        {
            return $"{FromId} {Type}➡️{ConnectionState} {ToId}";
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

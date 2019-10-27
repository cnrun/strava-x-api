using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Prototype.Model
{    
    [DataContract]
    class ActivityShort
    {
        [DataMember]
        ActivityType ActivityType;
        [DataMember]
        DateTime ActivityDate;
        [DataMember]
        string ActivityId;
        [DataMember]
        string ActivityTitle;
        [DataMember]
        string ActivityImageMapUrl;
        [DataMember]
        string AthleteId;

        [DataMember]
        List<String> ActivityImagesList;
        [DataMember]
        List<String> ActivityThumbnailsList;
        [DataMember]
        List<String> GroupActivityList;
        [DataMember]
        List<String> GroupAthleteList;

        public ActivityShort(
            string AthleteId,
            string ActivityId,
            string ActivityTitle,
            ActivityType ActivityType,
            DateTime ActivityDate,
            string ActivityImageMapUrl,
            List<String> ActivityThumbnailsList,
            List<String> ActivityImagesList,
            List<String> GroupActivityList,
            List<String> GroupAthleteList)
        {
            this.AthleteId = AthleteId;
            this.ActivityId = ActivityId;
            this.ActivityTitle = ActivityTitle;

            this.ActivityType = ActivityType;
            this.ActivityDate = ActivityDate;

            this.ActivityImageMapUrl=ActivityImageMapUrl;

            this.ActivityThumbnailsList = ActivityThumbnailsList;
            this.ActivityImagesList = ActivityImagesList;
            this.GroupActivityList = GroupActivityList;
            this.GroupAthleteList = GroupAthleteList;
        }
        override public string ToString()
        {
            return $"athlete:{AthleteId} activity:{ActivityId} text:'{ActivityTitle}' type:{ActivityType} date:{ActivityDate} Map {ActivityImageMapUrl} Images {ActivityThumbnailsList.Count}/{ActivityImagesList.Count} Group {GroupActivityList.Count}/{GroupAthleteList.Count}";
        }
        public static string WriteFromObject(ActivityShort ActivityShort)
        {
            // Create a stream to serialize the object to.
            var ms = new System.IO.MemoryStream();

            // Serializer the User object to the stream.
            // https://michaelscodingspot.com/the-battle-of-c-to-json-serializers-in-net-core-3/
            // The future of JSON in .NET Core 3.0 https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-apis/
            var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(ActivityShort));
            ser.WriteObject(ms, ActivityShort);
            byte[] json = ms.ToArray();
            ms.Close();
            return System.Text.Encoding.UTF8.GetString(json, 0, json.Length);
        }
    }
}

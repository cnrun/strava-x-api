using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Prototype.Model
{    
    public class ActivityShort
    {
        public ActivityType ActivityType { get; set; }
        public DateTime ActivityDate { get; set; }
        public DateTime ActivityLastCrawled { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ActivityId { get; set; }
        public string ActivityTitle { get; set; }
        public string ActivityImageMapUrl { get; set; }
        public string AthleteId { get; set; }

        // List as string: https://stackoverflow.com/a/31648135
        [Column]
        [Required]
        private String ActivityImagesListAsString { get; set; }
        [NotMapped]
        public List<String> ActivityImagesList { 
            get {
                return ActivityImagesListAsString.Split(',').ToList();
            }
            set {
                ActivityImagesListAsString = String.Join(",", value);
            }
        }
        [Column]
        [Required]
        private String ActivityThumbnailsListAsString { get; set; }
        [NotMapped]
        public List<String> ActivityThumbnailsList {
            get {
                return ActivityThumbnailsListAsString.Split(',').ToList();
            }
            set {
                ActivityThumbnailsListAsString = String.Join(",", value);
            }
        }
        [Column]
        [Required]
        private string GroupActivityListAsString { get; set; }
        [NotMapped]
        public List<String> GroupActivityList {
            get {
                return GroupActivityListAsString.Split(',').ToList();
            }
            set {
                GroupActivityListAsString = String.Join(",", value);
            }
        }
        [Column]
        [Required]
        private string GroupAthleteListAsString { get; set; }
        [NotMapped]
        public List<String> GroupAthleteList {
            get {
                return GroupActivityListAsString.Split(',').ToList();
            }
            set {
                GroupActivityListAsString = String.Join(",", value);
            }
        }
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

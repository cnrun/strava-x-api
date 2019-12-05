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
        public string StatShortString { get; set; }

        // List as string: https://stackoverflow.com/a/31648135

        //
        // ActivityImagesList
        //
        private List<String> _ActivityImagesList { get; set;}
        [NotMapped]
        public List<String> ActivityImagesList { 
            get {
                return _ActivityImagesList;
            }
            set {
                _ActivityImagesList = value;
            }
        }
        [Required]
        public String ActivityImagesListAsString
        { 
            get { return String.Join(',', _ActivityImagesList); }
            set { _ActivityImagesList = value.Split(',').ToList(); }
        }

        //
        // ActivityThumbnailsList
        //
        private List<String> _ActivityThumbnailsList { get; set;}
        [NotMapped]
        public List<String> ActivityThumbnailsList { 
            get {
                return _ActivityThumbnailsList;
            }
            set {
                _ActivityThumbnailsList = value;
            }
        }
        [Required]
        public String ActivityThumbnailsListAsString
        { 
            get { return String.Join(',', _ActivityThumbnailsList); }
            set { _ActivityThumbnailsList = value.Split(',').ToList(); }
        }

        //
        // GroupActivityList
        //
        private List<String> _GroupActivityList { get; set;}
        [NotMapped]
        public List<String> GroupActivityList { 
            get {
                return _GroupActivityList;
            }
            set {
                _GroupActivityList = value;
            }
        }
        [Required]
        public String GroupActivityListAsString
        { 
            get { return String.Join(',', _GroupActivityList); }
            set { _GroupActivityList = value.Split(',').ToList(); }
        }

        //
        // GroupActivityList
        //
        private List<String> _GroupAthleteList { get; set;}
        [NotMapped]
        public List<String> GroupAthleteList { 
            get {
                return _GroupAthleteList;
            }
            set {
                _GroupAthleteList = value;
            }
        }
        [Required]
        public String GroupAthleteListAsString
        { 
            get { return String.Join(',', _GroupAthleteList); }
            set { _GroupAthleteList = value.Split(',').ToList(); }
        }

        public ActivityShort()
        {
        }
        override public string ToString()
        {
            return $"athlete:{AthleteId} activity:{ActivityId} text:'{ActivityTitle}' type:{ActivityType} stats:{StatShortString} date:{ActivityDate} Map {ActivityImageMapUrl} Images {ActivityThumbnailsList.Count}/{ActivityImagesList.Count} Group {GroupActivityList.Count}/{GroupAthleteList.Count}";
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

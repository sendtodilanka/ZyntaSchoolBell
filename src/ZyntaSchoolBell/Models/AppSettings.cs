using Newtonsoft.Json;

namespace ZyntaSchoolBell.Models
{
    public class AppSettings
    {
        [JsonProperty("activeProfileId")]
        public string ActiveProfileId { get; set; } = "regular_day";

        [JsonProperty("volume")]
        public int Volume { get; set; } = 90;

        [JsonProperty("minimizeToTray")]
        public bool MinimizeToTray { get; set; } = true;

        [JsonProperty("mutedToday")]
        public bool MutedToday { get; set; }

        [JsonProperty("mutedDate")]
        public string MutedDate { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; } = 1;
    }
}

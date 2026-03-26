using System.Collections.Generic;
using Newtonsoft.Json;

namespace ZyntaSchoolBell.Models
{
    public class Profile
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; } = 1;

        [JsonProperty("alarms")]
        public List<AlarmEntry> Alarms { get; set; } = new List<AlarmEntry>();
    }
}

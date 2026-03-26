using System;
using Newtonsoft.Json;

namespace ZyntaSchoolBell.Models
{
    public class AlarmEntry
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("time")]
        public string Time { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("audioKey")]
        public string AudioKey { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;
    }
}

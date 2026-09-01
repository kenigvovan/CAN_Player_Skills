using Newtonsoft.Json;

namespace canplayerskills.Skills
{
    public class BackupConfig
    {
        [JsonProperty]
        public bool Enabled = true;

        /// <summary>Real minutes between snapshots. A snapshot is also taken when a player leaves.</summary>
        [JsonProperty]
        public int IntervalMinutes = 15;
    }
}

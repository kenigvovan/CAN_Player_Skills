using Newtonsoft.Json;

namespace canplayerskills.Skills
{
    public class ProspectingConfig
    {
        /// <summary>Half-edge of the scanned cube. Expensive on busy servers.</summary>
        [JsonProperty]
        public int SearchRadius = 12;

        [JsonProperty]
        public int CooldownMs = 1000;
    }
}

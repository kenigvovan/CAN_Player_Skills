using System.Collections.Generic;
using Newtonsoft.Json;

namespace canplayerskills.Skills
{
    public class SkillDef
    {
        [JsonIgnore]
        public string Code = "";

        /// <summary>Growth constant: multiplier is 1 - rate / (rate + raw). Higher rate = slower skill.</summary>
        [JsonProperty]
        public int Rate = 100;

        /// <summary>Raw skill points granted per successful action.</summary>
        [JsonProperty]
        public int XpPerAction = 1;

        /// <summary>Stat code -> [value at zero skill, value at full skill].</summary>
        [JsonProperty]
        public Dictionary<string, float[]> Stats = new Dictionary<string, float[]>();
    }
}

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

        /// <summary>
        /// Modid -> stat ranges that replace <see cref="Stats"/> while that mod is loaded, for mods
        /// that scale the same stat themselves and would otherwise stack with this one.
        /// </summary>
        [JsonProperty]
        public Dictionary<string, Dictionary<string, float[]>> StatOverrides
            = new Dictionary<string, Dictionary<string, float[]>>();
    }
}

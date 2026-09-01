using System.Collections.Generic;
using Newtonsoft.Json;

namespace canplayerskills.Skills
{
    /// <summary>
    /// One skill, as declared in assets/canplayerskills/config/skills.json.
    /// </summary>
    public class SkillDef
    {
        /// <summary>
        /// Filled in from the json dictionary key, not from the json body itself.
        /// </summary>
        [JsonIgnore]
        public string Code;

        /// <summary>
        /// Growth constant. The skill multiplier is 1 - rate / (rate + raw), so the multiplier
        /// reaches 0.5 exactly at raw == rate and approaches 1 asymptotically without ever hitting it.
        /// Higher rate == slower skill.
        /// </summary>
        [JsonProperty]
        public int Rate = 100;

        /// <summary>
        /// Entity stats driven by this skill: stat code -> value contributed at multiplier 1.0.
        /// The actual contribution is value * multiplier, applied under the "canplayerskills" source.
        /// </summary>
        [JsonProperty]
        public Dictionary<string, float> Stats = new Dictionary<string, float>();
    }
}

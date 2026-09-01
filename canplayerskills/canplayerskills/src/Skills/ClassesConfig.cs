using Newtonsoft.Json;

namespace canplayerskills.Skills
{
    public class ClassesConfig
    {
        /// <summary>Replace all character classes with a single traitless one.</summary>
        [JsonProperty]
        public bool CollapseToNeutral = false;

        [JsonProperty]
        public string NeutralCode = "neutral";
    }
}

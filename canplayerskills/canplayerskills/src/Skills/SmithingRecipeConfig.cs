using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace canplayerskills.Skills
{
    /// <summary>Smithing xp per recipe, keyed by output code. Wildcards allowed, first match wins.</summary>
    public class SmithingRecipeConfig
    {
        [JsonProperty]
        public SmithingRecipeEntry Default = new SmithingRecipeEntry();

        [JsonProperty]
        public Dictionary<string, SmithingRecipeEntry> ByOutput = new Dictionary<string, SmithingRecipeEntry>();

        public SmithingRecipeEntry EntryFor(AssetLocation? outputCode)
        {
            if (outputCode == null) return Default;

            foreach (KeyValuePair<string, SmithingRecipeEntry> pair in ByOutput)
            {
                if (WildcardUtil.Match(new AssetLocation(pair.Key), outputCode)) return pair.Value;
            }

            return Default;
        }
    }

    public class SmithingRecipeEntry
    {
        [JsonProperty]
        public int Xp = 1;

        /// <summary>Branches trained by this recipe. Each gets the full xp, as in TFC.</summary>
        [JsonProperty]
        public string[] Skills = new string[] { "smithing" };
    }
}

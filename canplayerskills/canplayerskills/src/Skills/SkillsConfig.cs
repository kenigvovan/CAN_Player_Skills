using System.Collections.Generic;
using Newtonsoft.Json;

namespace canplayerskills.Skills
{
    /// <summary>Admin config, ModConfig/canplayerskills.json, synced to clients on join.</summary>
    public class SkillsConfig
    {
        public const string ModConfigFile = "canplayerskills.json";
        public const string DefaultsAssetPath = "canplayerskills:config/defaults.json";

        [JsonProperty]
        public Dictionary<string, SkillDef> Skills = new Dictionary<string, SkillDef>();

        [JsonProperty]
        public SmithingRecipeConfig Smithing = new SmithingRecipeConfig();

        [JsonProperty]
        public ProspectingConfig Prospecting = new ProspectingConfig();

        [JsonProperty]
        public BackupConfig Backup = new BackupConfig();

        [JsonProperty]
        public ClassesConfig Classes = new ClassesConfig();
    }
}

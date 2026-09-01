using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace canplayerskills.Skills
{
    /// <summary>Lookup facade over the active <see cref="SkillsConfig"/>, replaceable at runtime.</summary>
    public class SkillRegistry
    {
        private SkillsConfig config = new SkillsConfig();

        public SkillsConfig Config => config;

        public IEnumerable<SkillDef> All => config.Skills.Values;

        public SmithingRecipeConfig Smithing => config.Smithing;

        public SkillDef? Get(string code)
        {
            SkillDef? def;
            return config.Skills.TryGetValue(code, out def) ? def : null;
        }

        public void Apply(SkillsConfig newConfig)
        {
            foreach (KeyValuePair<string, SkillDef> pair in newConfig.Skills)
            {
                pair.Value.Code = pair.Key;
            }

            config = newConfig;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(config);
        }

        public static SkillsConfig? FromJson(string json)
        {
            return JsonConvert.DeserializeObject<SkillsConfig>(json);
        }

        /// <summary>The values shipped with the mod, used until an admin config or a server packet arrives.</summary>
        public static SkillsConfig LoadDefaults(ICoreAPI api)
        {
            IAsset? asset = api.Assets.TryGet(new AssetLocation(SkillsConfig.DefaultsAssetPath));
            if (asset == null)
            {
                api.Logger.Error("[canplayerskills] {0} not found, no skills registered.", SkillsConfig.DefaultsAssetPath);
                return new SkillsConfig();
            }

            return asset.ToObject<SkillsConfig>() ?? new SkillsConfig();
        }
    }
}

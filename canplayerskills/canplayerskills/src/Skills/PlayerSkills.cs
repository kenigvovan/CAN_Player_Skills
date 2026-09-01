// Skill math ported from TerraFirmaCraft (GPL v3), com.bioxx.tfc.Core.Player.SkillStats.

using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace canplayerskills.Skills
{
    public static class PlayerSkills
    {
        public const string TreeKey = "canskills";

        public static int GetRaw(Entity entity, string skillCode)
        {
            ITreeAttribute? tree = entity.WatchedAttributes.GetTreeAttribute(TreeKey);
            return tree == null ? 0 : tree.GetInt(skillCode, 0);
        }

        public static void SetRaw(Entity entity, string skillCode, int value)
        {
            if (value < 0) value = 0;

            ITreeAttribute tree = entity.WatchedAttributes.GetOrAddTreeAttribute(TreeKey);
            tree.SetInt(skillCode, value);
            entity.WatchedAttributes.MarkPathDirty(TreeKey);
        }

        public static float GetMultiplier(int raw, int rate)
        {
            if (rate <= 0) return 0f;
            return 1f - (float)rate / (rate + raw);
        }

        public static EnumSkillRank GetRank(float multiplier)
        {
            if (multiplier < 0.25f) return EnumSkillRank.Novice;
            if (multiplier < 0.5f) return EnumSkillRank.Adept;
            if (multiplier < 0.75f) return EnumSkillRank.Expert;
            return EnumSkillRank.Master;
        }

        /// <summary>Progress 0..1 inside the current rank.</summary>
        public static float GetProgressInRank(float multiplier)
        {
            float floor = (int)GetRank(multiplier) * 0.25f;
            return (multiplier - floor) / 0.25f;
        }

        public static string RankLangKey(EnumSkillRank rank)
        {
            return "canplayerskills:skill-rank-" + rank.ToString().ToLowerInvariant();
        }
    }
}

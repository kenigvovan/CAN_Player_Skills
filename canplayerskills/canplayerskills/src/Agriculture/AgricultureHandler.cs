using canplayerskills.Skills;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace canplayerskills.Agriculture
{
    /// <summary>Xp and bonus drops for harvesting ripe crops.</summary>
    public class AgricultureHandler
    {
        public const string SkillCode = "agriculture";

        private readonly ICoreServerAPI sapi;
        private readonly canplayerskillsModSystem skills;

        public AgricultureHandler(ICoreServerAPI sapi, canplayerskillsModSystem skills)
        {
            this.sapi = sapi;
            this.skills = skills;

            sapi.Event.BreakBlock += OnBreakBlock;
            sapi.Event.DidBreakBlock += OnDidBreakBlock;
        }

        private void OnBreakBlock(IServerPlayer byPlayer, BlockSelection blockSel, ref float dropQuantityMultiplier, ref EnumHandling handling)
        {
            if (byPlayer == null) return;
            if (!IsRipeCrop(sapi.World.BlockAccessor.GetBlock(blockSel.Position))) return;

            dropQuantityMultiplier *= 1f + skills.GetMultiplier(byPlayer.Entity, SkillCode);
        }

        private void OnDidBreakBlock(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel)
        {
            if (byPlayer == null) return;
            if (!IsRipeCrop(sapi.World.GetBlock(oldblockId))) return;

            SkillDef? def = skills.Registry.Get(SkillCode);
            if (def == null) return;

            skills.Increase(byPlayer, SkillCode, def.XpPerAction);
        }

        public static bool IsRipeCrop(Block? block)
        {
            BlockCrop? crop = block as BlockCrop;
            return crop != null
                && crop.CropProps != null
                && crop.CurrentCropStage >= crop.CropProps.GrowthStages;
        }
    }
}

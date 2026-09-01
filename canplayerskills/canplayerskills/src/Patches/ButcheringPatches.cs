using canplayerskills.Skills;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace canplayerskills.Patches
{
    /// <summary>Butchering xp on the harvest that actually generates the drops.</summary>
    [HarmonyPatch(typeof(EntityBehaviorHarvestable), "SetHarvested")]
    public static class PatchButcheringHarvest
    {
        public const string SkillCode = "butchering";

        public static void Prefix(EntityBehaviorHarvestable __instance, out bool __state)
        {
            __state = __instance.entity.WatchedAttributes.GetBool("harvested", false);
        }

        public static void Postfix(EntityBehaviorHarvestable __instance, IPlayer byPlayer, bool __state)
        {
            if (__state) return;

            IWorldAccessor world = __instance.entity.World;
            if (world.Side != EnumAppSide.Server) return;

            IServerPlayer? splr = byPlayer as IServerPlayer;
            if (splr == null) return;

            canplayerskillsModSystem skills = world.Api.ModLoader.GetModSystem<canplayerskillsModSystem>();
            SkillDef? def = skills.Registry.Get(SkillCode);
            if (def == null) return;

            skills.Increase(splr, SkillCode, def.XpPerAction);
        }
    }
}

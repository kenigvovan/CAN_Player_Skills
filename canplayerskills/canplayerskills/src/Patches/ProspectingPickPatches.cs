using System.Linq;
using canplayerskills.Prospecting;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace canplayerskills.Patches
{
    /// <summary>Right click probes TFC-style. Patched on the base class with a type guard.</summary>
    [HarmonyPatch(typeof(CollectibleObject), "OnHeldInteractStart")]
    public static class PatchProPickInteract
    {
        public static bool Prefix(CollectibleObject __instance, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, ref EnumHandHandling handling)
        {
            if (!(__instance is ItemProspectingPick)) return true;
            if (blockSel == null || slot.Itemstack == null) return true;

            handling = EnumHandHandling.PreventDefault;

            IServerPlayer? splr = (byEntity as EntityPlayer)?.Player as IServerPlayer;
            if (splr == null) return false;

            long now = byEntity.World.ElapsedMilliseconds;
            if (now - slot.Itemstack.TempAttributes.GetLong("canLastProbeMs", 0) < ProspectingLogic.CooldownMs(byEntity.World)) return false;
            slot.Itemstack.TempAttributes.SetLong("canLastProbeMs", now);

            ProspectingLogic.Probe(byEntity.World, splr, blockSel.Position);
            __instance.DamageItem(byEntity.World, byEntity, slot, 1, true);

            return false;
        }
    }

    /// <summary>Breaking a block no longer probes; the pick acts as a plain tool.</summary>
    [HarmonyPatch(typeof(ItemProspectingPick), "OnBlockBrokenWith")]
    public static class PatchProPickBlockBroken
    {
        public static bool Prefix(ItemProspectingPick __instance, IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier, ref bool __result)
        {
            IPlayer? byPlayer = (byEntity as EntityPlayer)?.Player;

            (blockSel.Block ?? world.BlockAccessor.GetBlock(blockSel.Position))
                .OnBlockBroken(world, blockSel.Position, byPlayer, dropQuantityMultiplier);

            EnumItemDamageSource[] damagedBy = __instance.GetDamagedBy(itemslot);
            if (damagedBy != null && damagedBy.Contains(EnumItemDamageSource.BlockBreaking))
            {
                __instance.DamageItem(world, byEntity, itemslot, 1, true);
            }

            __result = true;
            return false;
        }
    }
}

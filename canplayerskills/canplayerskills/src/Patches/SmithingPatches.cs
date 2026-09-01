// Smithing bonus modelled on TerraFirmaCraft (GPL v3), cf. AnvilManager.getDurabilityBuff.

using System;
using System.Reflection;
using System.Text;
using canplayerskills.Skills;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace canplayerskills.Patches
{
    public static class SmithingBonus
    {
        public const string SkillCode = "smithing";
        public const string Attribute = "cansmithbonus";

        /// <summary>The smith's skill frozen onto the item at forging time, 0 if it has none.</summary>
        public static float Of(ItemStack? stack)
        {
            return stack == null ? 0f : stack.Attributes.GetFloat(Attribute, 0f);
        }
    }

    /// <summary>Replaces the vanilla finish step to stamp the crafter's skill onto the output stack.</summary>
    [HarmonyPatch(typeof(BlockEntityAnvil), "CheckIfFinished")]
    public static class PatchAnvilCheckIfFinished
    {
        // Fully qualified: Vintagestory.API.Common declares delegates of the same names.
        private static readonly System.Func<BlockEntityAnvil, bool>? matchesRecipe;
        private static readonly System.Action<BlockEntityAnvil>? regenMesh;

        static PatchAnvilCheckIfFinished()
        {
            MethodInfo? matches = AccessTools.Method(typeof(BlockEntityAnvil), "MatchesRecipe");
            if (matches != null) matchesRecipe = AccessTools.MethodDelegate<System.Func<BlockEntityAnvil, bool>>(matches);

            MethodInfo? regen = AccessTools.Method(typeof(BlockEntityAnvil), "RegenMeshAndSelectionBoxes");
            if (regen != null) regenMesh = AccessTools.MethodDelegate<System.Action<BlockEntityAnvil>>(regen);
        }

        public static bool Prefix(BlockEntityAnvil __instance, IPlayer byPlayer, ref ItemStack ___workItemStack)
        {
            if (matchesRecipe == null || regenMesh == null) return true;

            if (__instance.SelectedRecipe == null) return false;
            if (!matchesRecipe(__instance) || !(__instance.Api.World is IServerWorldAccessor)) return false;

            ItemStack workItem = ___workItemStack;
            if (workItem == null) return false;

            ICoreAPI api = __instance.Api;

            __instance.Voxels = new byte[16, 6, 16];

            ItemStack outstack = __instance.SelectedRecipe.Output.ResolvedItemstack.Clone();
            outstack.Collectible.SetTemperature(api.World, outstack,
                workItem.Collectible.GetTemperature(api.World, workItem), true);

            IServerPlayer? splr = byPlayer as IServerPlayer;
            canplayerskillsModSystem skills = api.ModLoader.GetModSystem<canplayerskillsModSystem>();
            SmithingRecipeEntry entry = skills.SmithingConfig.EntryFor(outstack.Collectible.Code);

            if (splr != null)
            {
                float bonus = skills.GetAverageMultiplier(splr.Entity, entry.Skills);
                if (bonus > 0f) outstack.Attributes.SetFloat(SmithingBonus.Attribute, bonus);
            }

            ___workItemStack = null;
            __instance.SelectedRecipeId = -1;

            if (byPlayer != null && byPlayer.InventoryManager.TryGiveItemstack(outstack, false))
            {
                api.World.PlaySoundFor(new AssetLocation("sounds/player/collect"), byPlayer, false, 24f, 1f);
                api.ModLoader.GetModSystem<ModSystemSubTongsDurability>(true).OnItemPickedUp(byPlayer.Entity, outstack);
            }
            else
            {
                api.World.SpawnItemEntity(outstack, __instance.Pos.ToVec3d().Add(0.5, 0.626, 0.5));
            }

            api.World.Logger.Audit("{0} Took 1x{1} from Anvil at {2}.",
                byPlayer == null ? null : byPlayer.PlayerName, outstack.Collectible.Code, __instance.Pos);

            regenMesh(__instance);
            __instance.MarkDirty(false, null);
            api.World.BlockAccessor.MarkBlockDirty(__instance.Pos, (IPlayer?)null);
            __instance.rotation = 0;

            if (splr != null && entry.Skills != null)
            {
                // Every branch the recipe trains gets the full amount, as in TFC.
                foreach (string skillCode in entry.Skills) skills.Increase(splr, skillCode, entry.Xp);
            }

            return false;
        }
    }

    /// <summary>Extra durability for items forged by a skilled smith. Armour gets this and nothing else.</summary>
    [HarmonyPatch(typeof(CollectibleObject), "GetMaxDurability")]
    public static class PatchSmithBonusDurability
    {
        public static void Postfix(ItemStack itemstack, ref int __result)
        {
            float bonus = SmithingBonus.Of(itemstack);
            if (bonus <= 0f) return;

            __result = (int)(__result * (1f + bonus));
        }
    }

    /// <summary>Extra attack power for weapons and tools forged by a skilled smith.</summary>
    [HarmonyPatch(typeof(CollectibleObject), "GetAttackPower")]
    public static class PatchSmithBonusDamage
    {
        public static void Postfix(ItemStack itemStack, ref float __result)
        {
            float bonus = SmithingBonus.Of(itemStack);
            if (bonus <= 0f) return;

            __result *= 1f + bonus;
        }
    }

    /// <summary>Tells the player why this piece is better than a plain one, cf. TFC's addSmithingBonusInformation.</summary>
    [HarmonyPatch(typeof(CollectibleObject), "GetHeldItemInfo")]
    public static class PatchSmithBonusTooltip
    {
        public static void Postfix(CollectibleObject __instance, ItemSlot inSlot, StringBuilder dsc)
        {
            ItemStack? stack = inSlot?.Itemstack;
            float bonus = SmithingBonus.Of(stack);
            if (bonus <= 0f || stack == null) return;

            string percent = (bonus * 100f).ToString("0");

            dsc.AppendLine(__instance.GetAttackPower(stack) > 0.5f
                ? Lang.Get("canplayerskills:smithbonus-durability-damage", percent)
                : Lang.Get("canplayerskills:smithbonus-durability", percent));
        }
    }
}

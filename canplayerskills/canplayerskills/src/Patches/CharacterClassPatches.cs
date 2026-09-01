using System.Reflection;
using canplayerskills.Skills;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace canplayerskills.Patches
{
    /// <summary>Collapses the class list to a single traitless class. Kept non-empty because other mods index into it.</summary>
    [HarmonyPatch(typeof(CharacterSystem), "loadCharacterClasses")]
    public static class PatchCollapseCharacterClasses
    {
        private static readonly MethodInfo? LoadMethod =
            AccessTools.Method(typeof(CharacterSystem), "loadCharacterClasses");

        public static void Postfix(CharacterSystem __instance, ICoreAPI ___api)
        {
            ClassesConfig cfg = ___api.ModLoader.GetModSystem<canplayerskillsModSystem>().Registry.Config.Classes;
            if (!cfg.CollapseToNeutral) return;

            CharacterClass neutral = new CharacterClass
            {
                Code = cfg.NeutralCode,
                Enabled = true,
                Traits = new string[0],
                Gear = new JsonItemStack[0]
            };

            __instance.characterClasses.Clear();
            __instance.characterClasses.Add(neutral);

            __instance.characterClassesByCode.Clear();
            __instance.characterClassesByCode[neutral.Code] = neutral;
        }

        /// <summary>Rebuilds the class list under the current config.</summary>
        public static void Reload(CharacterSystem charSys)
        {
            if (LoadMethod == null) return;

            // vanilla adds by key without clearing
            charSys.characterClassesByCode.Clear();
            LoadMethod.Invoke(charSys, null);
        }
    }

    /// <summary>Vanilla traits tab throws on an unknown class code; show nothing instead.</summary>
    [HarmonyPatch(typeof(CharacterSystem), "getClassTraitText")]
    public static class PatchTraitTextUnknownClass
    {
        public static bool Prefix(CharacterSystem __instance, ICoreAPI ___api, ref string __result)
        {
            EntityPlayer? entity = (___api as ICoreClientAPI)?.World?.Player?.Entity;
            if (entity == null) return true;

            string? code = entity.WatchedAttributes.GetString("characterClass", null);
            if (code != null && __instance.characterClassesByCode.ContainsKey(code)) return true;

            __result = "";
            return false;
        }
    }
}

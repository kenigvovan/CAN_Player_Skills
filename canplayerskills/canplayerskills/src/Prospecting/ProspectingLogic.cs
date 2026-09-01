// Ported from TerraFirmaCraft (GPL v3), com.bioxx.tfc.Items.Tools.ItemProPick.

using System;
using System.Collections.Generic;
using System.Linq;
using canplayerskills.Skills;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace canplayerskills.Prospecting
{
    public static class ProspectingLogic
    {
        public const string SkillCode = "prospecting";

        public static int CooldownMs(IWorldAccessor world)
        {
            return world.Api.ModLoader.GetModSystem<canplayerskillsModSystem>()
                .Registry.Config.Prospecting.CooldownMs;
        }

        public static void Probe(IWorldAccessor world, IServerPlayer splr, BlockPos pos)
        {
            canplayerskillsModSystem skills = world.Api.ModLoader.GetModSystem<canplayerskillsModSystem>();
            SkillDef? def = skills.Registry.Get(SkillCode);
            int xp = def == null ? 1 : def.XpPerAction;
            int radius = skills.Registry.Config.Prospecting.SearchRadius;

            EnumSkillRank rank = skills.GetRank(splr.Entity, SkillCode);
            string lang = splr.LanguageCode;

            Block targeted = world.BlockAccessor.GetBlock(pos);
            if (IsOre(targeted))
            {
                Message(splr, DirectHitText(lang, targeted, rank == EnumSkillRank.Master));
                skills.Increase(splr, SkillCode, xp);
                return;
            }

            // Deterministic per position: the same spot always yields the same reading.
            Random rnd = new Random(GameMath.MurmurHash3(pos.X, pos.Y, pos.Z));

            int chance = 60 + ((int)rank + 1) * 10;
            if (rank != EnumSkillRank.Master && rnd.Next(100) >= chance)
            {
                Message(splr, Lang.GetL(lang, "canplayerskills:propick-nothing"));
                return;
            }

            Dictionary<string, int> found = new Dictionary<string, int>();
            // WalkBlocks pulls every touched chunk into the block cache, keep the radius modest
            world.BlockAccessor.WalkBlocks(
                pos.AddCopy(-radius, -radius, -radius),
                pos.AddCopy(radius, radius, radius),
                (nblock, x, y, z) =>
                {
                    if (!IsOre(nblock)) return;

                    string key = "ore-" + nblock.Variant["type"];
                    int quantity;
                    found.TryGetValue(key, out quantity);
                    found[key] = quantity + 1;
                },
                false);

            if (found.Count == 0)
            {
                Message(splr, Lang.GetL(lang, "canplayerskills:propick-nothing"));
                return;
            }

            // TFC reports one randomly picked ore, not the whole list.
            List<string> keys = found.Keys.ToList();
            string picked = keys[rnd.Next(keys.Count)];

            Message(splr, Lang.GetL(lang, QuantityLangKey(found[picked]), Lang.GetL(lang, picked)));
            skills.Increase(splr, SkillCode, xp);
        }

        public static bool IsOre(Block? block)
        {
            return block != null
                && block.BlockMaterial == EnumBlockMaterial.Ore
                && block.Variant.ContainsKey("type");
        }

        private static string DirectHitText(string lang, Block ore, bool showGrade)
        {
            string oreName = Lang.GetL(lang, "ore-" + ore.Variant["type"]);
            string? grade = ore.Variant.ContainsKey("grade") ? ore.Variant["grade"] : null;

            if (showGrade && grade != null)
            {
                return Lang.GetL(lang, "canplayerskills:propick-found-exact-graded",
                    oreName, Lang.GetL(lang, "canplayerskills:oregrade-" + grade));
            }

            return Lang.GetL(lang, "canplayerskills:propick-found-exact", oreName);
        }

        private static string QuantityLangKey(int quantity)
        {
            if (quantity < 10) return "propick-nodesearch-traceamount";
            if (quantity < 20) return "propick-nodesearch-smallamount";
            if (quantity < 40) return "propick-nodesearch-mediumamount";
            if (quantity < 80) return "propick-nodesearch-largeamount";
            if (quantity < 160) return "propick-nodesearch-verylargeamount";
            return "propick-nodesearch-hugeamount";
        }

        private static void Message(IServerPlayer splr, string text)
        {
            splr.SendMessage(GlobalConstants.InfoLogChatGroup, text, EnumChatType.Notification);
        }
    }
}

using canplayerskills.Skills;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;

namespace canplayerskills.Gui
{
    /// <summary>Skills tab in the character dialog, one row per skill.</summary>
    public class SkillsTab
    {
        private const double RowWidth = 385;
        private const double BarWidth = 355;
        private const double TextHeight = 18;
        private const double BarHeight = 8;
        private const double NameToBarGap = 6;
        private const double Gap = 9;

        private static readonly double[] BarColor = new double[] { 0.4, 0.6, 0.9, 1.0 };

        private static readonly string[] RankColor = new string[]
        {
            "#B0B0B0", "#7BC96F", "#6FA8FF", "#FFCC55"
        };

        private readonly ICoreClientAPI capi;
        private readonly canplayerskillsModSystem skills;

        // kept until composing is done, statbar values need element bounds
        private GuiComposer? pendingComposer;

        private GuiDialogCharacterBase? charDlg;

        public SkillsTab(ICoreClientAPI capi, canplayerskillsModSystem skills)
        {
            this.capi = capi;
            this.skills = skills;
        }

        /// <summary>Register on LevelFinalize, after other mods added their tabs.</summary>
        public void Register()
        {
            if (charDlg != null) return;

            GuiDialogCharacterBase? dlg = capi.Gui.LoadedGuis
                .Find(g => g is GuiDialogCharacterBase) as GuiDialogCharacterBase;

            if (dlg == null)
            {
                capi.Logger.Warning("[canplayerskills] Character dialog not found, no skills tab added.");
                return;
            }

            // tab/renderer lists already out of sync, don't make it worse
            if (dlg.Tabs.Count != dlg.RenderTabHandlers.Count)
            {
                capi.Logger.Warning("[canplayerskills] Character dialog has {0} tabs but {1} render handlers, no skills tab added.",
                    dlg.Tabs.Count, dlg.RenderTabHandlers.Count);
                return;
            }

            dlg.Tabs.Add(new GuiTab
            {
                Name = Lang.Get("canplayerskills:charactertab-skills"),
                DataInt = dlg.Tabs.Count
            });

            dlg.RenderTabHandlers.Add(Compose);
            dlg.ComposeExtraGuis += FillBars;

            charDlg = dlg;
        }

        public void Unregister()
        {
            if (charDlg == null) return;

            charDlg.ComposeExtraGuis -= FillBars;
            charDlg = null;
            pendingComposer = null;
        }

        private void Compose(GuiComposer composer)
        {
            ElementBounds row = ElementBounds.Fixed(0, 20, RowWidth, TextHeight);

            foreach (SkillDef def in skills.Registry.All)
            {
                composer.AddRichtext(NameAndRank(def), CairoFont.WhiteSmallishText(), row);

                ElementBounds bar = row.BelowCopy(0, NameToBarGap).WithFixedSize(BarWidth, BarHeight);
                composer.AddStatbar(bar, BarColor, "bar-" + def.Code);

                row = bar.BelowCopy(0, Gap).WithFixedSize(RowWidth, TextHeight);
            }

            pendingComposer = composer;
        }

        private void FillBars()
        {
            GuiComposer? composer = pendingComposer;
            pendingComposer = null;

            // Fires for every tab; only ours leaves a composer behind.
            if (composer == null) return;

            Entity? entity = capi.World.Player?.Entity;
            if (entity == null) return;

            foreach (SkillDef def in skills.Registry.All)
            {
                float mult = PlayerSkills.GetMultiplier(PlayerSkills.GetRaw(entity, def.Code), def.Rate);
                composer.GetStatbar("bar-" + def.Code)?.SetValues(PlayerSkills.GetProgressInRank(mult) * 100f, 0f, 100f);
            }
        }

        private string NameAndRank(SkillDef def)
        {
            EnumSkillRank rank = PlayerSkills.GetRank(MultiplierOf(def));

            return Lang.Get("canplayerskills:skill-" + def.Code)
                + ": <font color=\"" + RankColor[(int)rank] + "\">"
                + Lang.Get(PlayerSkills.RankLangKey(rank)) + "</font>";
        }

        private float MultiplierOf(SkillDef def)
        {
            Entity? entity = capi.World.Player?.Entity;
            return entity == null ? 0f : PlayerSkills.GetMultiplier(PlayerSkills.GetRaw(entity, def.Code), def.Rate);
        }
    }
}

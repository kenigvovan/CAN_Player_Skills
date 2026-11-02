using System;
using System.Collections.Generic;
using System.Text;
using canplayerskills.Agriculture;
using canplayerskills.Gui;
using canplayerskills.Network;
using canplayerskills.Patches;
using canplayerskills.Skills;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace canplayerskills
{
    public class canplayerskillsModSystem : ModSystem
    {
        public const string StatSource = "canplayerskills";
        public const string HarmonyId = "canplayerskills";

        private Harmony? harmony;
        private SkillsTab? skillsTab;
        private ICoreAPI? api;
        private ICoreServerAPI? sapi;
        private ICoreClientAPI? capi;

        public SkillRegistry Registry { get; } = new SkillRegistry();

        public SmithingRecipeConfig SmithingConfig => Registry.Smithing;

        public override void Start(ICoreAPI api)
        {
            this.api = api;

            api.Network.RegisterChannel(SkillsConfigPacket.ChannelName)
                .RegisterMessageType<SkillsConfigPacket>();

            if (!Harmony.HasAnyPatches(HarmonyId))
            {
                harmony = new Harmony(HarmonyId);
                harmony.PatchAll();
            }
        }

        public override void Dispose()
        {
            skillsTab?.Unregister();
            skillsTab = null;

            harmony?.UnpatchAll(HarmonyId);
            harmony = null;
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            // defaults first, then the admin config; must run after assets are loaded
            ApplyConfig(SkillRegistry.LoadDefaults(api));

            if (api is ICoreServerAPI serverApi)
            {
                LoadAdminConfig(serverApi);
                RegisterBackup(serverApi);
            }
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            skillsTab = new SkillsTab(api, this);

            // On LevelFinalize so tabs added by other mods are already in place.
            api.Event.LevelFinalize += skillsTab.Register;

            api.Network.GetChannel(SkillsConfigPacket.ChannelName)
                .SetMessageHandler<SkillsConfigPacket>(OnConfigReceived);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;

            // On join, not on now-playing: a new player reaches now-playing only after closing the
            // character dialog, which the client builds from this config's class list.
            api.Event.PlayerJoin += SendConfigTo;
            api.Event.PlayerNowPlaying += OnPlayerNowPlaying;
            new AgricultureHandler(api, this);
            RegisterCommands(api);
        }

        /// <summary>Reads ModConfig/canplayerskills.json, writes the defaults there on first run.</summary>
        private void LoadAdminConfig(ICoreServerAPI api)
        {
            SkillsConfig? saved;

            try
            {
                saved = api.LoadModConfig<SkillsConfig>(SkillsConfig.ModConfigFile);
            }
            catch (System.Exception e)
            {
                // keep the previous config, leave the broken file for the admin
                api.Logger.Error("[canplayerskills] {0} could not be read, keeping the previous config: {1}",
                    SkillsConfig.ModConfigFile, e.Message);
                return;
            }

            if (saved == null)
            {
                api.StoreModConfig(Registry.Config, SkillsConfig.ModConfigFile);
                api.Logger.Notification("[canplayerskills] Wrote default config to {0}.", SkillsConfig.ModConfigFile);
                return;
            }

            ApplyConfig(saved);
        }

        /// <summary>
        /// Installs a config, then lets every loaded mod named in a skill's StatOverrides replace the
        /// ranges it balances itself. Butchering is the case this exists for: it halves field
        /// harvesting and pays the rest out at its own workstations, both reading animalLootDropRate,
        /// so scaling that stat here as well would multiply the two reductions.
        /// </summary>
        private void ApplyConfig(SkillsConfig config)
        {
            Registry.Apply(config);

            foreach (SkillDef def in Registry.All)
            {
                foreach (KeyValuePair<string, Dictionary<string, float[]>> mod in def.StatOverrides)
                {
                    if (api?.ModLoader.IsModEnabled(mod.Key) != true) continue;

                    foreach (KeyValuePair<string, float[]> stat in mod.Value)
                    {
                        def.Stats[stat.Key] = stat.Value;
                    }
                }
            }
        }

        private void RegisterBackup(ICoreServerAPI api)
        {
            BackupConfig cfg = Registry.Config.Backup;
            if (!cfg.Enabled) return;

            int intervalMs = Math.Max(1, cfg.IntervalMinutes) * 60 * 1000;
            api.Event.RegisterGameTickListener(dt => SkillsBackup.Save(api, Registry), intervalMs);

            api.Event.PlayerDisconnect += byPlayer => SkillsBackup.Save(api, Registry);
        }

        private void OnPlayerNowPlaying(IServerPlayer byPlayer)
        {
            MigrateCharacterClass(byPlayer);
            ApplyStats(byPlayer.Entity);
        }

        /// <summary>Moves players off a class code that no longer exists.</summary>
        private void MigrateCharacterClass(IServerPlayer byPlayer)
        {
            if (byPlayer.Entity == null) return;

            CharacterSystem? charSys = sapi?.ModLoader.GetModSystem<CharacterSystem>();
            if (charSys == null || charSys.characterClasses.Count == 0) return;

            string? current = byPlayer.Entity.WatchedAttributes.GetString("characterClass", null);
            if (current != null && charSys.characterClassesByCode.ContainsKey(current)) return;

            ClassesConfig cfg = Registry.Config.Classes;

            // no class yet: let the vanilla selection dialog handle it
            if (current == null && !cfg.CollapseToNeutral) return;

            string target = cfg.CollapseToNeutral
                ? cfg.NeutralCode
                : (charSys.characterClasses.Find(c => c.Enabled) ?? charSys.characterClasses[0]).Code;

            charSys.setCharacterClass(byPlayer.Entity, target, false);
        }

        private void SendConfigTo(IServerPlayer byPlayer)
        {
            sapi?.Network.GetChannel(SkillsConfigPacket.ChannelName)
                .SendPacket(new SkillsConfigPacket { Json = Registry.ToJson() }, byPlayer);
        }

        private void OnConfigReceived(SkillsConfigPacket packet)
        {
            SkillsConfig? received = SkillRegistry.FromJson(packet.Json);
            if (received == null) return;

            ApplyConfig(received);

            // rebuild the class list under the server config
            CharacterSystem? charSys = capi?.ModLoader.GetModSystem<CharacterSystem>();
            if (charSys != null) PatchCollapseCharacterClasses.Reload(charSys);
        }

        public float GetMultiplier(Entity entity, string skillCode)
        {
            SkillDef? def = Registry.Get(skillCode);
            if (def == null) return 0f;
            return PlayerSkills.GetMultiplier(PlayerSkills.GetRaw(entity, skillCode), def.Rate);
        }

        public EnumSkillRank GetRank(Entity entity, string skillCode)
        {
            return PlayerSkills.GetRank(GetMultiplier(entity, skillCode));
        }

        /// <summary>Mean multiplier over several skills, as TFC averages the skills a recipe trains.</summary>
        public float GetAverageMultiplier(Entity entity, string[]? skillCodes)
        {
            if (skillCodes == null || skillCodes.Length == 0) return 0f;

            float sum = 0f;
            foreach (string code in skillCodes) sum += GetMultiplier(entity, code);

            return sum / skillCodes.Length;
        }

        public void Increase(IServerPlayer byPlayer, string skillCode, int amount)
        {
            if (amount == 0) return;

            if (Registry.Get(skillCode) == null)
            {
                sapi?.Logger.Warning("[canplayerskills] Cannot award {0} xp to {1}: no skill '{2}' in the config.",
                    amount, byPlayer.PlayerName, skillCode);
                return;
            }

            Entity entity = byPlayer.Entity;
            EnumSkillRank before = GetRank(entity, skillCode);

            PlayerSkills.SetRaw(entity, skillCode, PlayerSkills.GetRaw(entity, skillCode) + amount);
            ApplyStats(entity);

            EnumSkillRank after = GetRank(entity, skillCode);
            if (after != before)
            {
                string lang = byPlayer.LanguageCode;
                byPlayer.SendMessage(
                    GlobalConstants.InfoLogChatGroup,
                    Lang.GetL(lang, "canplayerskills:skill-rankup",
                        Lang.GetL(lang, "canplayerskills:skill-" + skillCode),
                        Lang.GetL(lang, PlayerSkills.RankLangKey(after))),
                    EnumChatType.Notification);
            }
        }

        public void ApplyStats(Entity entity)
        {
            foreach (SkillDef def in Registry.All)
            {
                float mult = PlayerSkills.GetMultiplier(PlayerSkills.GetRaw(entity, def.Code), def.Rate);

                foreach (KeyValuePair<string, float[]> stat in def.Stats)
                {
                    if (stat.Value == null || stat.Value.Length < 2) continue;

                    float target = stat.Value[0] + (stat.Value[1] - stat.Value[0]) * mult;

                    // Stats blend as a weighted sum over a base of 1, so contribute the difference.
                    entity.Stats.Set(stat.Key, StatSource + ":" + def.Code, target - 1f, false);
                }
            }
        }

        /// <summary>One line per skill: name, rank, progress inside the rank and raw xp.</summary>
        private string SkillsReport(Entity entity, string lang)
        {
            StringBuilder sb = new StringBuilder();

            foreach (SkillDef def in Registry.All)
            {
                int raw = PlayerSkills.GetRaw(entity, def.Code);
                float mult = PlayerSkills.GetMultiplier(raw, def.Rate);

                sb.AppendLine(Lang.GetL(lang, "canplayerskills:skill-line",
                    Lang.GetL(lang, "canplayerskills:skill-" + def.Code),
                    Lang.GetL(lang, PlayerSkills.RankLangKey(PlayerSkills.GetRank(mult))),
                    (PlayerSkills.GetProgressInRank(mult) * 100f).ToString("0"),
                    raw));
            }

            return sb.ToString();
        }

        private void RegisterCommands(ICoreServerAPI api)
        {
            api.ChatCommands.Create("skill")
                .WithDescription(Lang.Get("canplayerskills:command-skill-desc"))
                .RequiresPrivilege(Privilege.chat)
                .RequiresPlayer()
                .HandleWith(args =>
                {
                    IServerPlayer? plr = args.Caller.Player as IServerPlayer;
                    if (plr == null) return TextCommandResult.Error(Lang.Get("canplayerskills:msg-player-only"));

                    return TextCommandResult.Success(SkillsReport(plr.Entity, plr.LanguageCode));
                })
                .BeginSubCommand("show")
                    .WithDescription(Lang.Get("canplayerskills:command-show-desc"))
                    .RequiresPrivilege(Privilege.controlserver)
                    .WithArgs(api.ChatCommands.Parsers.OnlinePlayer("player"))
                    .HandleWith(args =>
                    {
                        IPlayer? target = args[0] as IPlayer;
                        if (target?.Entity == null) return TextCommandResult.Error(Lang.Get("canplayerskills:msg-player-not-found"));

                        string lang = (args.Caller.Player as IServerPlayer)?.LanguageCode ?? Lang.CurrentLocale;

                        return TextCommandResult.Success(
                            Lang.GetL(lang, "canplayerskills:msg-skills-of", target.PlayerName)
                            + "\n" + SkillsReport(target.Entity, lang));
                    })
                .EndSubCommand()
                .BeginSubCommand("give")
                    .WithDescription(Lang.Get("canplayerskills:command-give-desc"))
                    .RequiresPrivilege(Privilege.controlserver)
                    .WithArgs(
                        api.ChatCommands.Parsers.OnlinePlayer("player"),
                        api.ChatCommands.Parsers.Word("skill"),
                        api.ChatCommands.Parsers.Int("amount"))
                    .HandleWith(args =>
                    {
                        IServerPlayer? target = args[0] as IServerPlayer;
                        string skillCode = (string)args[1];
                        int amount = (int)args[2];

                        if (target == null) return TextCommandResult.Error(Lang.Get("canplayerskills:msg-player-not-found"));
                        if (Registry.Get(skillCode) == null)
                        {
                            return TextCommandResult.Error(Lang.Get("canplayerskills:msg-unknown-skill", skillCode));
                        }

                        Increase(target, skillCode, amount);
                        return TextCommandResult.Success(
                            Lang.Get("canplayerskills:msg-skill-given", skillCode, amount, target.PlayerName));
                    })
                .EndSubCommand()
                .BeginSubCommand("restore")
                    .WithDescription(Lang.Get("canplayerskills:command-restore-desc"))
                    .RequiresPrivilege(Privilege.controlserver)
                    .WithArgs(api.ChatCommands.Parsers.OnlinePlayer("player"))
                    .HandleWith(args =>
                    {
                        IServerPlayer? target = args[0] as IServerPlayer;
                        if (target?.Entity == null) return TextCommandResult.Error(Lang.Get("canplayerskills:msg-player-not-found"));

                        BackupEntry? entry = SkillsBackup.Load(api).EntryFor(target.PlayerUID);
                        if (entry == null) return TextCommandResult.Error(Lang.Get("canplayerskills:msg-no-backup", target.PlayerName));

                        foreach (KeyValuePair<string, int> pair in entry.Skills)
                        {
                            PlayerSkills.SetRaw(target.Entity, pair.Key, pair.Value);
                        }

                        ApplyStats(target.Entity);

                        return TextCommandResult.Success(
                            Lang.Get("canplayerskills:msg-restored", target.PlayerName, entry.SavedAt));
                    })
                .EndSubCommand()
                .BeginSubCommand("backup")
                    .WithDescription(Lang.Get("canplayerskills:command-backup-desc"))
                    .RequiresPrivilege(Privilege.controlserver)
                    .HandleWith(args =>
                    {
                        SkillsBackup.Save(api, Registry);
                        return TextCommandResult.Success(
                            Lang.Get("canplayerskills:msg-backed-up", SkillsBackup.FileNameFor(api)));
                    })
                .EndSubCommand()
                .BeginSubCommand("reload")
                    .WithDescription(Lang.Get("canplayerskills:command-reload-desc"))
                    .RequiresPrivilege(Privilege.controlserver)
                    .HandleWith(args =>
                    {
                        LoadAdminConfig(api);

                        foreach (IPlayer plr in api.World.AllOnlinePlayers)
                        {
                            IServerPlayer? splr = plr as IServerPlayer;
                            if (splr == null) continue;

                            SendConfigTo(splr);
                            ApplyStats(splr.Entity);
                        }

                        return TextCommandResult.Success(
                            Lang.Get("canplayerskills:msg-reloaded", new List<SkillDef>(Registry.All).Count));
                    })
                .EndSubCommand();
        }
    }
}

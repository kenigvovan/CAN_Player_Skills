using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace canplayerskills.Skills
{
    /// <summary>Per-savegame copy of everyone's xp in ModConfig, survives world rollbacks.</summary>
    public class SkillsBackup
    {
        [JsonProperty]
        public Dictionary<string, BackupEntry> Players = new Dictionary<string, BackupEntry>();

        public static string FileNameFor(ICoreServerAPI api)
        {
            return "canplayerskills-backup-" + api.WorldManager.SaveGame.SavegameIdentifier + ".json";
        }

        public static SkillsBackup Load(ICoreServerAPI api)
        {
            try
            {
                return api.LoadModConfig<SkillsBackup>(FileNameFor(api)) ?? new SkillsBackup();
            }
            catch (Exception e)
            {
                api.Logger.Error("[canplayerskills] Skill backup could not be read: {0}", e.Message);
                return new SkillsBackup();
            }
        }

        /// <summary>Merges online players into the file, offline entries are kept.</summary>
        public static void Save(ICoreServerAPI api, SkillRegistry registry)
        {
            SkillsBackup backup = Load(api);
            string now = DateTime.UtcNow.ToString("u");

            foreach (IPlayer plr in api.World.AllOnlinePlayers)
            {
                if (plr.Entity == null) continue;

                BackupEntry entry = new BackupEntry
                {
                    PlayerName = plr.PlayerName,
                    SavedAt = now
                };

                foreach (SkillDef def in registry.All)
                {
                    entry.Skills[def.Code] = PlayerSkills.GetRaw(plr.Entity, def.Code);
                }

                backup.Players[plr.PlayerUID] = entry;
            }

            try
            {
                api.StoreModConfig(backup, FileNameFor(api));
            }
            catch (Exception e)
            {
                api.Logger.Error("[canplayerskills] Skill backup could not be written: {0}", e.Message);
            }
        }

        public BackupEntry? EntryFor(string playerUid)
        {
            BackupEntry? entry;
            return Players.TryGetValue(playerUid, out entry) ? entry : null;
        }
    }

    public class BackupEntry
    {
        [JsonProperty]
        public string PlayerName = "";

        [JsonProperty]
        public string SavedAt = "";

        [JsonProperty]
        public Dictionary<string, int> Skills = new Dictionary<string, int>();
    }
}

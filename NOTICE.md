# Attribution

canplayerskills is licensed under the GNU General Public License v3 (see LICENSE.txt).

Parts of this mod are ported from **TerraFirmaCraft** for Minecraft 1.7.10
(https://github.com/Deadrik/TFCraft), Copyright (c) Bioxx, Dunkleosteus, Kittychanley and
contributors, licensed under GPL v3.

Ported logic:

- Skill progression math (`1 - rate / (rate + raw)`), rank thresholds and per-skill rates
  — from `com.bioxx.tfc.Core.Player.SkillStats` and `com.bioxx.tfc.TerraFirmaCraft`
  → `src/Skills/PlayerSkills.cs`, `assets/canplayerskills/config/skills.json`
- Prospecting pick behaviour (position-seeded roll, rank-based success chance, 24³ scan,
  single random result, quantity buckets) — from `com.bioxx.tfc.Items.Tools.ItemProPick`
  → `src/Prospecting/ProspectingLogic.cs`
- Smithing bonus (crafter's skill stamped on the finished item, read back as extra max durability
  and attack power) — from `com.bioxx.tfc.api.Crafting.AnvilManager`, `TEAnvil` and `ItemTerraTool`
  → `src/Patches/SmithingPatches.cs`
- Butchering yield scaling by skill (TFC: `weight * (multiplier + 0.01)`; the range is widened here
  because Vintage Story rounds small drop counts down to nothing) — from
  `com.bioxx.tfc.Handlers.EntityLivingHandler` → `assets/canplayerskills/config/defaults.json`

The code is a reimplementation in C# against the Vintage Story API, not a copy of the Java source.

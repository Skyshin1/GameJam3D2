using System;
using System.Collections.Generic;

namespace AnchorDefense
{
    public static partial class GameLocalization
    {
        // Defaults keep existing ScriptableObject assets intact. Each asset can override its English copy.
        private static readonly Dictionary<string, string> AssetNames = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "anchor_strike", "Anchor Strike" }, { "repair_pulse", "Repair Pulse" },
            { "repulsion_wave", "Repulsion Wave" }, { "rotate_ring", "Ring Control" },
            { "slow_field", "Stasis Field" }, { "power_field", "Firepower Boost" },
            { "rapid_field", "Rapid Fire" },
            { "turret.projectile.radius", "Proximity Fuse" },
            { "turret.projectile.speed", "Magnetic Acceleration" },
            { "ring.inner.capacity", "Inner Ring Expansion" },
            { "ring.middle.capacity", "Middle Ring Expansion" },
            { "ring.outer.capacity", "Outer Ring Expansion" },
            { "turret.armor.01", "Deflection Armor" },
            { "turret.damage.01", "Anchor Amplification" },
            { "turret.health.01", "Anchor Armor Protocol" },
            { "turret.interval.01", "Synchronized Reload" },
            { "turret.range.01", "Deep-Space Targeting" },
            { "turret.recovery.01", "Autonomous Restart" },
            { "zone.fragment.damage", "Green Fire-Control Fragment" },
            { "zone.fragment.health", "Amber Vitality Fragment" },
            { "zone.fragment.range", "Orange Horizon Fragment" },
            { "zone.fragment.repair", "Cyan Repair Fragment" },
            { "zone.fragment.vulnerability", "Violet Fracture Fragment" },
            { "zone.turret.acceleration", "Blue: Turret Overclock" },
            { "zone.turret.repair", "Cyan: Nanite Repair" },
            { "zone.turret.damage", "Green: Focused Fire" },
            { "zone.turret.range", "Orange: Extended Horizon" },
            { "zone.enemy.vulnerability", "Violet: Structural Fracture" },
            { "zone.enemy.suppression", "Red: Enemy Suppression" },
            { "zone.turret.health", "Yellow: Vital Anchor" }
        };

        private static readonly Dictionary<string, string> AssetDescriptions = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "anchor_strike", "Damage every enemy in the selected zone. Best against crowded areas. Keywords: attack, strike, blast, damage, kill enemies." },
            { "repair_pulse", "Restore health to all living turrets in the selected zone. Keywords: repair, heal, damaged turrets." },
            { "repulsion_wave", "Push enemies in the selected zone away from the planetary core. Keywords: repel, push, knock back." },
            { "rotate_ring", "Take control of an existing orbital ring and rotate its turrets. Continue until stopped or manually overridden unless one turn is explicitly requested. Supports inner, middle and outer rings, direction, angle and repeat count." },
            { "slow_field", "Create a persistent slowing field in the selected zone, affecting current and incoming enemies. Keywords: slow, stasis, freeze." },
            { "power_field", "Increase turret projectile damage in the selected zone while the command field lasts. Keywords: boost damage, firepower." },
            { "rapid_field", "Increase turret fire rate in the selected zone while the command field lasts. Keywords: rapid fire, attack speed." },
            { "turret.projectile.radius", "Increase projectile hit radius by 35%." },
            { "turret.projectile.speed", "Increase all projectile speeds by 30%. Requires Deep-Space Targeting." },
            { "ring.inner.capacity", "Add two turret slots to the inner ring." },
            { "ring.middle.capacity", "Add two turret slots to the middle ring. Requires Inner Ring Expansion." },
            { "ring.outer.capacity", "Add two turret slots to the outer ring. Requires Middle Ring Expansion." },
            { "turret.armor.01", "All turrets take 20% less damage. Requires Autonomous Restart." },
            { "turret.damage.01", "Increase all turret projectile damage by 25%." },
            { "turret.health.01", "Increase all turret maximum health by 30%." },
            { "turret.interval.01", "Reduce all turret firing intervals by 15%. Requires Anchor Amplification." },
            { "turret.range.01", "Increase all turret targeting range by 20%." },
            { "turret.recovery.01", "Reduce turret disable duration by 30%. Requires Anchor Armor Protocol." },
            { "zone.fragment.damage", "Unlock the green zone effect: turrets in the zone deal 50% more damage." },
            { "zone.fragment.health", "Unlock the yellow zone effect: allied turrets in the zone gain 50% maximum health." },
            { "zone.fragment.range", "Unlock the orange zone effect: turrets in the zone gain 35% range. Requires Amber Vitality Fragment." },
            { "zone.fragment.repair", "Unlock the cyan zone effect: working turrets in the zone recover 4 health per second. Requires Amber Vitality Fragment." },
            { "zone.fragment.vulnerability", "Unlock the violet zone effect: enemies in the zone take 35% more damage. Requires Green Fire-Control Fragment." },
            { "zone.turret.acceleration", "Turrets in the zone fire 30% faster." },
            { "zone.turret.repair", "Working turrets in the zone restore 4 health per second." },
            { "zone.turret.damage", "Turrets in the zone deal 50% more damage." },
            { "zone.turret.range", "Turrets in the zone gain 35% range." },
            { "zone.enemy.vulnerability", "Enemies in the zone take 35% more damage." },
            { "zone.enemy.suppression", "Enemies in the zone move 35% slower and take 4 damage per second." },
            { "zone.turret.health", "All allied turrets in the zone gain 50% maximum health." }
        };

        public static string AssetName(string id, string overrideText, string chineseFallback) =>
            !string.IsNullOrWhiteSpace(overrideText) ? overrideText :
            id != null && AssetNames.TryGetValue(id, out string text) ? text : chineseFallback;

        public static string AssetDescription(string id, string overrideText, string chineseFallback) =>
            !string.IsNullOrWhiteSpace(overrideText) ? overrideText :
            id != null && AssetDescriptions.TryGetValue(id, out string text) ? text : chineseFallback;

        public static string[] SkillEnglishKeywords(string id)
        {
            switch (id)
            {
                case "anchor_strike": return new[] { "strike", "attack", "blast", "bombard" };
                case "repair_pulse": return new[] { "repair", "heal", "restore" };
                case "repulsion_wave": return new[] { "repel", "push", "knock back" };
                case "rotate_ring": return new[] { "rotate ring", "control ring", "spin ring", "stop ring" };
                case "slow_field": return new[] { "slow", "stasis", "freeze" };
                case "power_field": return new[] { "boost damage", "firepower" };
                case "rapid_field": return new[] { "rapid fire", "fire rate", "attack speed" };
                default: return Array.Empty<string>();
            }
        }

    }
}

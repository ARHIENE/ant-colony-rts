using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Save
{
    internal static class SaveOrderValidation
    {
        private static void Check(bool valid, string field) { if (!valid) throw new InvalidOperationException("Invalid saved order: " + field); }
        private static bool F(float n) => !float.IsNaN(n) && !float.IsInfinity(n) && Mathf.Abs(n) <= 1e8f;
        private static bool N(float n) => F(n) && n >= 0;
        private static bool V(Vec3Dto v) => v != null && v.IsFinite();
        internal static void Validate(SaveFileV1 f)
        {
            var keys = new HashSet<string>();
            void Add(string key) { Check(keys.Add(key), "duplicate reference " + key); }
            void Building(string key, int nodes) { Add("building:" + key); for (int i = 0; i < nodes; i++) Add("building:" + key + ":node:" + i); }
            for (int i = 0; i < f.commanders.Count; i++) Add("commander:" + i);
            for (int i = 0; i < f.world.transports.Count; i++) Add("ship:" + i);
            foreach (var d in f.buildings) Building(d.key, d.nodes.Count);
            foreach (var d in f.nodes) Add("node:" + d.key);
            foreach (var d in f.construction ?? new List<ConstructionDto>()) {
                Add("construction:" + f.construction.IndexOf(d));
                Check(d.needs.All(n => n.reserved >= 0 && (long)n.reserved + n.delivered <= n.need), "construction reservation");
            }
            for (int i = -1; i < f.world.sites.Count; i++) {
                var c = i < 0 ? f.world.homeColony : f.world.sites[i].colony;
                if (c != null) foreach (var n in c.nodes) Add("node:" + n.key);
                if (c != null) for (int j = 0; j < c.buildingHealth.Count; j++) {
                    // Enemy-colony resource node keys are also present in f.nodes; these aliases cover building targets.
                    Add("building:colony:" + i + ":" + j);
                }
            }
            foreach (var d in f.monsters) Add(d.key);
            foreach (var a in f.events.actors) {
                var key = "event:" + (int)a.kind + ":" + JsonUtility.ToJson(a.position);
                if (!keys.Contains(key)) Add(key);
            }
            void Ref(string key) { Check(string.IsNullOrEmpty(key) || keys.Contains(key), "reference " + key); }
            bool Site(int i) => i >= -1 && i < f.world.sites.Count;
            foreach (var c in f.commanders) {
                if (!c.hasOrder) continue;
                var d = c.order; Check(d != null, "commander order");
                var s = d.soldier; var w = d.worker;
                Check(s != null && w != null && s.state >= 0 && s.state <= 3 && V(s.destination) && V(s.attackDestination)
                    && F(s.attackTimer) && F(s.searchTimer), "soldier");
                Check(w.state >= 0 && w.state <= 8 && Enum.IsDefined(typeof(ResourceType), w.fetchType) && w.fetchAmount >= 0 && N(w.buildTimer), "worker");
                Check(F(d.huntCooldown) && F(d.workScan) && (d.serviceJob == 0 || Units.CommanderWorkState.Index((Units.CommanderJobs)d.serviceJob) >= 0), "commander work");
                foreach (var key in new[] { s.target, w.node, w.deposit, w.construction, w.delivery, w.fetchHost, w.fishingNode, d.service, d.hunt, d.automaticFacility }) Ref(key);
                Check(w.state != 1 && w.state != 2 || !string.IsNullOrEmpty(w.node), "gather target");
                Check(w.state != 3 && w.state != 4 || !string.IsNullOrEmpty(w.deposit), "deposit target");
                Check(w.state != 5 && w.state != 6 || !string.IsNullOrEmpty(w.construction), "construction target");
                Check(w.state != 7 && w.state != 8 || !string.IsNullOrEmpty(w.delivery) && w.fetchAmount > 0, "delivery target");
            }
            for (int i = 0; i < f.world.transports.Count; i++) {
                var d = f.world.transports[i].route;
                Check(d.crew != null && d.crew.Count <= f.commanders.Count && d.crew.Distinct().Count() == d.crew.Count
                    && d.crew.All(id => id >= 0 && id < f.commanders.Count && f.commanders[id].location == 1 && f.commanders[id].transportIndex == i), "crew ownership");
                // Empty crew is the legacy format, reconstructed from commander ownership.
                Check(d.crew.Count == 0 || d.crew.Count == f.commanders.Count(c => c.location == 1 && c.transportIndex == i), "crew completeness");
                Check(d.nextNode == null || d.nodes != null && d.nodes.Count <= 10000 && d.nextNode.Length == d.crew.Count
                    && d.nextNode.All(n => n >= 0 && n <= d.nodes.Count), "route cursor");
                if (d.nodes != null) foreach (var key in d.nodes) Ref(key);
            }
            foreach (var d in f.monsters.Where(m => m.hasRuntime)) {
                var m = d.runtime;
                Check(m != null && m.maxHealth > 0 && N(m.maxHealth) && d.health <= m.maxHealth && Site(m.parentSite) && Site(m.raidSite)
                    && m.invasionSite >= -2 && m.invasionSite < f.world.sites.Count && Site(m.defenseSite), "monster owner/health");
                Check(new[] { m.detection, m.damage, m.range, m.interval, m.searchInterval, m.speed, m.root }.All(N)
                    && V(m.scale) && F(m.rotationY) && F(m.attackTimer) && F(m.searchTimer) && F(m.siegeCheck) && N(m.captureChance) && m.captureChance <= 1, "monster timers/config");
                Check(!m.commander || d.traits != null && m.roles != null && m.roles.Count > 0 && m.roles.Count <= 6
                    && m.roles.All(r => Enum.IsDefined(typeof(UnitRole), r)) && Enum.IsDefined(typeof(CommanderRank), m.rank), "enemy commander");
                Check(d.key.StartsWith("monster:") || d.key.StartsWith("runtime:") || d.key.StartsWith("event:") || d.key.StartsWith("critter:") || d.key.StartsWith("rebel:"), "monster key");
                if (d.key.StartsWith("runtime:")) Check(int.TryParse(d.key.Substring(8), out var runtimeId) && runtimeId >= 0, "runtime key");
                if (d.key.StartsWith("rebel:")) Check(f.diplomacy.rebelMembers.Any(r => "rebel:" + r.commander.PersonalState.id == d.key), "rebel identity");
                if (d.key.StartsWith("critter:")) Check(f.critters != null && f.critters.Any(c => "critter:" + c.id == d.key), "critter identity");
                if (d.key.StartsWith("event:")) Check(f.events.actors.Any(a => "event:" + (int)a.kind + ":" + JsonUtility.ToJson(a.position) == d.key), "event identity");
                Ref(m.target);
            }
            foreach (var s in f.world.sites)
                Check(!s.defenseUnderAttack || f.monsters.Any(m => m.hasRuntime && m.runtime.defenseSite == s.index && !m.runtime.allied && m.health > 0), "active defense attackers");
        }
    }
}

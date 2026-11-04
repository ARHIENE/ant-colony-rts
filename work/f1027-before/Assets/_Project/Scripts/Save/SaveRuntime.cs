using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AntColony.Save
{
    internal static class SaveRuntime
    {
        internal static void RegisterMonsters()
        {
            for (int i = 0; i < SaveCatalog.Monsters.Length; i++) SaveReferences.Register("monster:" + i, SaveCatalog.Monsters[i]);
            int next = 0;
            foreach (var m in SaveCatalog.Ordered<WildMonster>().Where(m => !SaveCatalog.Monsters.Contains(m)
                && m.GetComponent<EventActor>() == null && m.GetComponent<Critter>() == null && string.IsNullOrEmpty(m.RebelId)))
                SaveReferences.Register("runtime:" + next++, m);
            for (int i = 0; i < SaveCatalog.Bosses.Length; i++) SaveReferences.Register("boss:" + i, SaveCatalog.Bosses[i]);
        }
        internal static void CaptureMonsters(SaveFileV1 file)
        {
            for (int i = 0; i < SaveCatalog.Monsters.Length; i++)
                file.monsters.Add(Capture(SaveCatalog.Monsters[i], "monster:" + i));
            foreach (var m in SaveCatalog.Ordered<WildMonster>().Where(m => !SaveCatalog.Monsters.Contains(m)))
                file.monsters.Add(Capture(m, SaveReferences.Key(m)));
        }
        private static MonsterDto Capture(WildMonster m, string key) => new MonsterDto { key = key,
            health = m != null ? m.CurrentHealth : 0, position = new Vec3Dto(m != null ? m.Position : Vector3.zero),
            hasRuntime = m != null, runtime = m != null ? m.CaptureRuntime() : null, diplomaticFactionId = m?.DiplomaticFactionId,
            temperament = m != null ? m.Temperament : WildlifeTemperament.Predator, huntDesignated = m != null && m.HuntDesignated,
            traits = m is EnemyCommander c ? SaveCatalog.Traits(c.Traits) : null,
            talents = m is EnemyCommander ec ? ec.Talents.Copy() : null };

        internal static void RestoreMonsters(SaveFileV1 file)
        {
            var world = WorldMapManager.Instance;
            var restored = new Dictionary<string, WildMonster>();
            foreach (var d in file.monsters.Where(d => d.hasRuntime))
            {
                var state = d.runtime;
                WildMonster m;
                if (d.key.StartsWith("monster:")) m = SaveCatalog.Monsters[int.Parse(d.key.Substring(8))];
                else if (!d.key.StartsWith("runtime:")) m = SaveReferences.Get<WildMonster>(d.key);
                else
                {
                    var parent = state.parentSite < 0 ? null : world.Sites[state.parentSite].transform;
                    var invasion = state.invasionSite < -1 ? null : state.invasionSite < 0
                        ? AntColony.Core.SceneObjects.All<ColonyInvasion>().FirstOrDefault(c => c.GetComponentInParent<ExpeditionSite>() == null)
                        : world.Sites[state.invasionSite].Colony?.GetComponent<ColonyInvasion>();
                    if (invasion != null) m = invasion.RestoreSpawn(state.commander, d.position.ToVector3());
                    else if (state.visitor && AntColony.Core.SceneObjects.Any<LocalIncursions>() is LocalIncursions local)
                        m = local.RestoreSpawn(state.commander, d.position.ToVector3());
                    else if (state.commander)
                    {
                        var template = state.parentSite >= 0 ? world.Sites[state.parentSite].GuardTemplate
                            : world.Sites.FirstOrDefault(s => s.GuardTemplate != null)?.GuardTemplate;
                        if (template == null) throw new InvalidOperationException("Missing runtime commander template.");
                        m = Object.Instantiate(template, d.position.ToVector3(), Quaternion.identity, parent);
                    }
                    else
                    {
                        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule); go.SetActive(false);
                        go.transform.SetParent(parent); go.transform.position = d.position.ToVector3();
                        m = go.AddComponent<WildMonster>();
                    }
                }
                if (m == null) { if (d.health > 0) throw new InvalidOperationException("Missing saved enemy: " + d.key); continue; }
                m.gameObject.SetActive(false); m.Agent.enabled = false;
                m.transform.SetParent(state.parentSite < 0 ? null : world.Sites[state.parentSite].transform, true);
                m.name = state.name; m.transform.position = d.position.ToVector3();
                m.transform.rotation = Quaternion.Euler(0, state.rotationY, 0); m.transform.localScale = state.scale.ToVector3();
                if (state.raider) m.MakeRaider();
                m.DiplomaticFactionId = d.diplomaticFactionId; m.Temperament = d.temperament; m.HuntDesignated = d.huntDesignated;
                if (m is EnemyCommander c && d.traits != null) {
                    c.ConfigureCommander(state.commanderName, (CommanderRank)state.rank, state.roles.Select(r => (UnitRole)r).ToArray(), SaveCatalog.Traits(d.traits));
                    c.RestoreTalents(d.talents); c.CaptureChance = state.captureChance;
                }
                // OnEnable initializes health. Apply saved health and combat timers after activation; never Die/TakeDamage.
                m.RestoreHealth(d.health); m.RestoreRuntime(state);
                SaveReferences.Register(d.key, m); restored[d.key] = m;
                if (state.visitor) AntColony.Core.SceneObjects.Any<LocalIncursions>()?.RestoreVisitor(m);
                if (state.invasionSite >= -1) {
                    var colony = state.invasionSite < 0 ? AntColony.Core.SceneObjects.All<EnemyColony>()
                        .FirstOrDefault(cn => cn.GetComponentInParent<ExpeditionSite>() == null) : world.Sites[state.invasionSite].Colony;
                    colony?.GetComponent<ColonyInvasion>()?.RestoreRaider(m);
                }
            }
            foreach (var d in file.monsters.Where(d => d.hasRuntime))
                if (restored.TryGetValue(d.key, out var m)) m.RestoreRuntime(d.runtime, true);
            for (int i = 0; i < world.Sites.Count; i++) {
                var site = file.world.sites[i];
                world.Sites[i].Defense?.RestoreAttack(site.defenseUnderAttack, site.defenseFaction,
                    file.monsters.Where(d => d.hasRuntime && d.runtime.defenseSite == i && d.health > 0)
                        .Select(d => restored[d.key]));
            }
        }
    }
}

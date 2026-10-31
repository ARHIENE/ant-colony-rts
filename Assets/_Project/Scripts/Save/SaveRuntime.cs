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
            guardVersion = 1, guardBoss = m != null ? m.GuardBoss : -1, guardCycle = m != null ? m.GuardCycle : 0, guardXpClaimed = m != null && m.GuardXpClaimed,
            investigationId = (m as EnemyCommander)?.InvestigationId,
            health = m != null ? m.CurrentHealth : 0, position = new Vec3Dto(m != null ? m.Position : Vector3.zero),
            hasRuntime = m != null, runtime = m != null ? m.CaptureRuntime() : null, diplomaticFactionId = m?.DiplomaticFactionId,
            temperament = m != null ? m.Temperament : WildlifeTemperament.Predator, huntDesignated = m != null && m.HuntDesignated,
            traits = m is EnemyCommander c ? SaveCatalog.Traits(c.Traits) : null,
            talents = m is EnemyCommander ec ? ec.Talents.Copy() : null };

        // 저장 키는 숫자 순서로 읽는다. 방문대는 마지막 장수가 파동을 닫는다.
        internal static IEnumerable<(MonsterDto monster, int member)> MilitaryMembers(IEnumerable<MonsterDto> monsters)
        {
            var next = new Dictionary<(int parent, int defense, int invasion, bool visitor, string faction), int>();
            foreach (var d in monsters.Where(d => d.hasRuntime && d.guardBoss < 0 && !d.runtime.allied
                && !d.runtime.docile && !d.runtime.wasp && !d.runtime.night && string.IsNullOrEmpty(d.runtime.rebel)
                && (d.key.StartsWith("monster:") || d.key.StartsWith("runtime:"))
                && (d.runtime.parentSite >= 0 || d.runtime.visitor || d.runtime.invasionSite >= -1))
                .OrderBy(d => d.key.StartsWith("monster:") ? 0 : 1)
                .ThenBy(d => int.Parse(d.key.Substring(d.key.IndexOf(':') + 1))))
            {
                var r = d.runtime;
                var group = (r.parentSite, r.defenseSite, r.invasionSite, r.visitor, d.diplomaticFactionId);
                next.TryGetValue(group, out int member);
                // 기존 생성 번호는 앞 장수가 사망해도 유지된다.
                var name = r.visitor || r.defenseSite >= 0 ? r.name : r.commanderName;
                var marker = r.visitor ? " 침공군 " : r.defenseSite >= 0 ? " Invader " : " Commander ";
                int at = name?.LastIndexOf(marker, StringComparison.Ordinal) ?? -1;
                if (at >= 0 && int.TryParse(name.Substring(at + marker.Length), out int number) && number > 0)
                    member = number - 1;
                next[group] = r.visitor && r.commander ? 0 : member + 1;
                yield return (d, member);
            }
        }

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
                m.GuardBoss = d.guardBoss; m.GuardCycle = d.guardCycle; m.GuardXpClaimed = d.guardXpClaimed;
                if (m.GuardBoss < 0 && state.name == "보스 호위" && m.GetComponentInParent<BossStronghold>() is BossStronghold stronghold)
                {
                    m.GuardBoss = (int)stronghold.Id; m.GuardCycle = CampaignResearch.Instance?.BossProgress.Cycle(stronghold.Id) ?? 0;
                    m.GuardXpClaimed = true; // 구저장 처치 보상 여부 불명: 소급 지급하지 않는다.
                }
                m.transform.rotation = Quaternion.Euler(0, state.rotationY, 0); m.transform.localScale = state.scale.ToVector3();
                if (state.raider) m.MakeRaider();
                m.DiplomaticFactionId = d.diplomaticFactionId; m.Temperament = d.temperament; m.HuntDesignated = d.huntDesignated;
                if (m is EnemyCommander c && d.traits != null) {
                    c.ConfigureCommander(state.commanderName, (CommanderRank)state.rank, state.roles.Select(r => (UnitRole)r).ToArray(), SaveCatalog.Traits(d.traits));
                    c.RestoreTalents(d.talents); c.CaptureChance = state.captureChance; c.InvestigationId = d.investigationId;
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
            foreach (var (d, member) in MilitaryMembers(file.monsters))
            {
                if (!restored.TryGetValue(d.key, out var m)) continue;
                var faction = !string.IsNullOrEmpty(d.diplomaticFactionId)
                    ? DiplomacyManager.Instance.Data.civilizations.Find(c => c.id == d.diplomaticFactionId)
                    : d.runtime.parentSite >= 0 && world.Sites[d.runtime.parentSite].Disposition != ConquestDisposition.Annexed
                        ? DiplomacyManager.Instance.Faction(world.Sites[d.runtime.parentSite]) : null;
                if (faction == null) continue;
                // 편성의 상한 처리 뒤 저장 체력을 적용한다. 회복/절삭하지 않는다.
                m.RestoreAIMilitary(faction.era, member, d.health);
                m.Agent.stoppingDistance = AIEraMilitary.Range(AIEraMilitary.Weapon(faction.era, member));
            }
            for (int i = 0; i < world.Sites.Count; i++) {
                var site = file.world.sites[i];
                world.Sites[i].Defense?.RestoreAttack(site.defenseUnderAttack, site.defenseFaction,
                    file.monsters.Where(d => d.hasRuntime && d.runtime.defenseSite == i && d.health > 0)
                        .Select(d => restored[d.key]));
            }
        }
    }
}

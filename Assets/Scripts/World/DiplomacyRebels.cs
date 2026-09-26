using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    [Serializable] public sealed class RebelMember
    {
        public string faction;
        public Prisoner commander;
        public int troops, site;
        public float health;
        public Vector3 position;
    }
    public sealed partial class DiplomacyManager
    {
        private readonly List<EnemyCommander> rebelActors = new List<EnemyCommander>();
        public bool ReceiveDeparture(CommanderAnt commander, bool rebellion)
        {
            var c = Data.civilizations.Find(x => x.rebel && !x.extinct);
            if (c == null && !rebellion) return false;
            if (c == null)
            {
                c = new Civilization { id = "rebel:" + commander.PersonalState.id, name = commander.CommanderName + " 반란군", leader = commander.CommanderName,
                    rebel = true, contacted = true, war = true, affinity = -60, founded = Data.elapsed, warStarted = Data.elapsed,
                    nextRaid = Data.elapsed + 3 * DiplomacyRules.Month, agenda = LeaderAgenda.Conqueror, hiddenAgenda = LeaderAgenda.Warrior,
                    color = Color.HSVToRGB((Data.civilizations.Count * .137f) % 1, .8f, .95f) };
                Data.civilizations.Add(c);
                var site = world.Sites.FirstOrDefault(s => Faction(s) == null && s.Visitor == null && s.Disposition != ConquestDisposition.Annexed
                    && (s.Kind == ExpeditionSiteKind.ResourceSite && s.Cleared || s.Disposition == ConquestDisposition.Abandoned));
                if (site == null)
                {
                    site = world.CreateRebelSite(Data.extraSites);
                    Data.extraSites++; Data.owners.Add("");
                }
                Data.owners[world.Sites.ToList().IndexOf(site)] = c.id;
                CampaignHistory.Record("반란 세력", c.name, site.Title + " 점거", true);
            }
            var home = world.Sites.FirstOrDefault(s => Faction(s) == c);
            if (home == null) return false;
            var prisoner = new Prisoner(commander.CommanderName, CommanderRank.Sergeant, new[] { commander.Role }, commander.Traits)
            {
                Talents = commander.Talents.Copy(), PersonalState = commander.CapturePersonalState(), LabAttack = commander.LabAttackLevel,
                LabArmor = commander.LabArmorLevel, StrikeCooldown = commander.Skills.PowerStrikeCooldownLeft, StanceCooldown = commander.Skills.DefensiveStanceCooldownLeft
            };
            prisoner.PersonalState.originFaction = c.id;
            c.rebels.Add(prisoner);
            var member = new RebelMember { faction = c.id, commander = prisoner, troops = commander.TroopCount,
                health = Mathf.Max(1, commander.TroopCount * 10), site = world.Sites.ToList().IndexOf(home), position = home.Landing + Vector3.forward * (8 + c.rebels.Count * 2) };
            Data.rebelMembers.Add(member); SpawnRebel(member);
            CommanderRoster.Instance.Forget(commander);
            commander.PersonalState.equipment.Clear();
            Destroy(commander.gameObject);
            return true;
        }
        private void SpawnRebel(RebelMember member)
        {
            var site = world.Sites[member.site];
            var enemy = Instantiate(site.GuardTemplate, member.position, Quaternion.identity, site.transform);
            enemy.RebelId = member.commander.PersonalState.id; enemy.DiplomaticFactionId = member.faction;
            enemy.ConfigureCommander(member.commander.Name, member.commander.Rank, member.commander.Roles, member.commander.Traits);
            enemy.RestoreTalents(member.commander.Talents); enemy.ConfigureForce(Mathf.Max(1, member.troops * 10));
            enemy.gameObject.SetActive(true); enemy.RestoreHealth(member.health); rebelActors.Add(enemy);
            var faction = Data.civilizations.Find(c => c.id == member.faction);
            var color = new MaterialPropertyBlock(); color.SetColor("_BaseColor", faction.color); color.SetColor("_Color", faction.color);
            foreach (var renderer in enemy.GetComponentsInChildren<Renderer>()) renderer.SetPropertyBlock(color);
        }
        public bool DefeatRebel(string id)
        {
            var member = Data.rebelMembers.Find(m => m.commander.PersonalState.id == id);
            if (member == null) return false;
            var c = Data.civilizations.Find(f => f.id == member.faction);
            member.commander.PersonalState.social.departure = DepartureState.Imprisoned;
            var captured = PrisonerCamp.Instance?.ReceiveForTrade(member.commander) == true;
            if (!captured && member.commander.PersonalState.equipment.Count > 0) EquipmentLoot.Drop(member.position, member.commander.PersonalState.equipment);
            c.rebels.RemoveAll(p => p.PersonalState.id == id); Data.rebelMembers.Remove(member); c.playerScore += 10;
            CampaignHistory.Record(captured ? "포로" : "사망", member.commander.Name, "반란군 제압", true);
            CheckRebelExtinction(c); return captured;
        }
        private void CheckRebelExtinction(Civilization c)
        {
            if (!c.rebel || c.rebels.Count > 0) return;
            c.extinct = true; c.war = false;
            for (var i = 0; i < Data.owners.Count; i++) if (Data.owners[i] == c.id) Data.owners[i] = "";
            CampaignHistory.Record("세력 소멸", c.name, "소속 장수 없음", true);
        }
        internal void CaptureRebels()
        {
            foreach (var actor in rebelActors.Where(a => a != null && !a.IsDead))
            {
                var member = Data.rebelMembers.Find(m => m.commander.PersonalState.id == actor.RebelId);
                if (member != null) { member.health = actor.CurrentHealth; member.position = actor.Position; }
            }
        }
        internal void RestoreRebels()
        { foreach (var member in Data.rebelMembers) SpawnRebel(member); }
        private void RemoveTradedRebels(Civilization c, TradeOffer take)
        {
            foreach (var actor in rebelActors.Where(a => a != null && take.prisoners.Contains(a.RebelId)).ToArray()) { rebelActors.Remove(actor); Destroy(actor.gameObject); }
            Data.rebelMembers.RemoveAll(m => take.prisoners.Contains(m.commander.PersonalState.id)); CheckRebelExtinction(c);
        }
    }
}

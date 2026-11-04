using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Map;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.UI
{
    public static class ThreatIncidents
    {
        public static void Scan(bool announce)
        {
            var world = WorldMapManager.Instance;
            if (world == null) return;
            var active = new Dictionary<string, string>();
            string Region(Vector3 position)
            {
                foreach (var site in world.Sites)
                    if ((site.transform.position - position).sqrMagnitude < 70 * 70) return site.Title;
                return "본거지";
            }
            bool VisibleThreat(Vector3 position, bool combat)
            {
                if (HomeMapBuilder.CurrentWorldBounds.Contains(new Vector3(position.x, 0, position.z)) || combat) return true;
                return world.Sites.Any(s => s.Visitor != null && s.Visitor.State == ExpeditionState.Deployed
                    && (s.transform.position - position).sqrMagnitude < 70 * 70);
            }
            foreach (var m in WildMonster.All)
                if (m != null && CombatTargeting.CanAttack(AntColony.Data.UnitRole.Ranged, m) && VisibleThreat(m.Position, m.InCombat))
                { var region = Region(m.Position); active["threat:" + region] = region + ": 새 위협"; }
            foreach (var c in AntUnitBase.Active.OfType<CommanderAnt>())
                if (c.IsHostile && CombatTargeting.CanAttack(AntColony.Data.UnitRole.Ranged, c) && VisibleThreat(c.Position, c.IsInCombat))
                { var region = Region(c.Position); active["threat:" + region] = region + ": 새 위협"; }
            foreach (var site in world.Sites)
                if (site.Boss != null && !site.Boss.IsDead && site.Visitor != null && site.Visitor.State == ExpeditionState.Deployed)
                    active["threat:" + site.Title] = site.Title + ": 보스 위협";
            if (CommanderRoster.Instance != null)
                foreach (var c in CommanderRoster.Instance.Commanders)
                    if (c != null && c.IsColonyMember && (c.PersonalState.health.Downed || c.PersonalHealth <= 0))
                        active["down:" + c.PersonalState.id] = c.CommanderName + ": 쓰러짐";
            SimulationPause.Observe(active, announce);
            foreach (var rocket in AntColony.Core.SceneObjects.All<AirshipYard>())
                if (rocket.GetComponent<RocketIncident>() == null) rocket.gameObject.AddComponent<RocketIncident>();
        }
    }

    public sealed class RocketIncident : MonoBehaviour
    {
        private AirshipYard rocket;
        private string id;
        private RocketPhase observedPhase;
        private string cycle;
        private void Awake()
        {
            rocket = GetComponent<AirshipYard>(); id = "rocket:" + rocket.GetEntityId();
            observedPhase = Save.SaveSystem.Busy ? rocket.Phase : RocketPhase.Construction;
        }
        private void Update()
        {
            if (Save.SaveSystem.Busy) { observedPhase = rocket.Phase; return; }
            if (rocket.Operating) cycle = ":" + rocket.FixedSnapshot?.at.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            if (rocket.Phase == observedPhase) return;
            observedPhase = rocket.Phase;
            if (observedPhase == RocketPhase.Preparing)
                SimulationPause.Incident(id + cycle + ":activated", $"로켓 가동 — 준비 {rocket.PreparationRemaining:0}초, 최종 공세 시작.", UserSettings.Current.autoPauseRocket);
            else if (observedPhase == RocketPhase.Boarding && rocket.PreparationRemaining <= 0)
                SimulationPause.Incident(id + cycle + ":ready", "로켓 준비 완료 — 탑승 가능. 발사까지 공세가 계속됩니다.", UserSettings.Current.autoPauseRocket);
            else if (observedPhase == RocketPhase.Destroyed) Destroyed();
        }
        private void Destroyed() => SimulationPause.Incident(id + cycle + ":destroyed",
            "로켓이 파괴되었습니다. 평시 위기는 3일 후 재개됩니다.", UserSettings.Current.autoPauseRocket);
        private void OnDisable()
        {
            if (Application.isPlaying && !Save.SaveSystem.Busy && rocket != null && rocket.IsDead
                && CampaignResearch.Instance?.Departed != true)
                Destroyed();
        }
    }
}

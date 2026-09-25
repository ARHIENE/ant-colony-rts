using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.Save
{
    internal static class SaveBuildings
    {
        internal static BuildingDto Capture(BuildingBase b, string key, bool built, List<CommanderAnt> commanders)
        {
            if (b == null) return new BuildingDto { key = key, kind = "Destroyed", health = 0 };
            var d = new BuildingDto { key = key, kind = SaveCatalog.Kind(b), runtimeBuilt = built,
                position = new Vec3Dto(b.Position), rotationY = b.transform.eulerAngles.y, health = b.CurrentHealth };
            if (b is Barracks barracks) { d.role = (int)barracks.Role; d.barracksTier = barracks.CurrentTier; d.barracksUpgradeRemaining = barracks.UpgradeRemaining; }
            if (b is ResearchLab lab) { d.role = (int)lab.Role; d.labResearchRemaining = lab.ResearchRemaining;
                d.labResearchCommanderId = commanders.IndexOf(lab.Target); d.labResearchAttack = lab.ResearchIsAttack; }
            if (b is QueenChamber queen) { d.queenProductionRemaining = queen.ProductionRemaining; d.queenFishingRemaining = queen.FishingRemaining; }
            if (b is DigSite dig) d.digExpanded = dig.IsExpanded;
            if (b is AcidTower tower) d.towerCooldown = tower.Cooldown;
            if (b is ScienceLab science) { d.scienceRemaining = science.Remaining; d.scienceAircraft = science.Aircraft;
                d.scienceConstructing = science.Constructing; d.scienceSpawn = new Vec3Dto(science.SpawnPosition);
                d.scienceTier = science.Tier; d.scientist = commanders.IndexOf(science.Target); }
            if (b is AirshipYard yard) d.airship = yard.CaptureState(commanders);
            if (b is Infirmary infirmary) d.patients = infirmary.Patients.Select(c => commanders.IndexOf(c)).ToList();
            var scout = b.GetComponent<ScoutPost>();
            if (scout != null) { d.scoutRemaining = scout.Remaining; d.scoutDispatched = scout.IsDispatched;
                d.scoutDispatchedAnts = scout.DispatchedAnts; d.scoutSuccess = scout.SuccessCount; d.scoutFailure = scout.FailureCount; }
            var prison = b.GetComponent<PrisonerCamp>();
            if (prison != null) { d.prisonEscapeTimer = prison.EscapeTimer; d.prisonRecruited = prison.RecruitedCount;
                d.prisonExecuted = prison.ExecutedCount; d.prisonEscaped = prison.EscapedCount;
                foreach (var p in prison.Prisoners) d.prisoners.Add(new PrisonerDto { name = p.Name, rank = (int)p.Rank,
                    roles = p.Roles.Select(r => (int)r).ToList(), traits = SaveCatalog.Traits(p.Traits), talents = p.Talents.Copy(), persuadeAttempts = p.PersuadeAttempts }); }
            var nursery = b.GetComponent<NurseryChamber>();
            if (nursery != null) { d.nurseryBirths = nursery.BirthCount;
                var first = new List<CommanderAnt>(); var second = new List<CommanderAnt>(); var values = new List<float>();
                nursery.CaptureAffinity(first, second, values);
                for (var i = 0; i < values.Count; i++) d.nurseryAffinity.Add(new AffinityDto {
                    firstCommanderId = commanders.IndexOf(first[i]), secondCommanderId = commanders.IndexOf(second[i]), value = values[i] }); }
            var nodes = b.GetComponentsInChildren<ResourceNode>(true);
            for (var i = 0; i < nodes.Length; i++) d.nodes.Add(new ResourceNodeDto { index = i, amount = nodes[i].AmountRemaining, regrowTimer = nodes[i].RegrowTimeRemaining });
            return d;
        }

        internal static BuildingBase Resolve(BuildingDto d)
        {
            if (!d.runtimeBuilt) return SaveCatalog.Buildings[int.Parse(d.key)];
            var template = BuildingPlacementController.GetTemplate(Enum.Parse<BuildingKind>(d.kind), (UnitRole)d.role);
            if (template == null) throw new InvalidOperationException("Missing building template: " + d.kind);
            var go = UnityEngine.Object.Instantiate(template, d.position.ToVector3(), Quaternion.Euler(0, d.rotationY, 0));
            go.name = d.kind;
            go.SetActive(true);
            return go.GetComponent<BuildingBase>();
        }

        internal static void Restore(BuildingBase b, BuildingDto d, List<CommanderAnt> commanders)
        {
            if (d.kind == "Destroyed") { if (b != null) { b.gameObject.SetActive(false); UnityEngine.Object.Destroy(b.gameObject); } return; }
            if (b == null) throw new InvalidOperationException("Missing building: " + d.key);
            b.transform.SetPositionAndRotation(d.position.ToVector3(), Quaternion.Euler(0, d.rotationY, 0));
            b.RestoreHealth(d.health);
            if (b is Barracks barracks) barracks.RestoreState(d.barracksTier, d.barracksUpgradeRemaining);
            if (b is ResearchLab lab && d.labResearchCommanderId >= 0) lab.RestoreState(commanders[d.labResearchCommanderId], d.labResearchAttack, d.labResearchRemaining);
            if (b is QueenChamber queen) queen.RestoreState(d.queenProductionRemaining, d.queenFishingRemaining);
            if (b is DigSite dig) dig.RestoreExpanded(d.digExpanded);
            if (b is AcidTower tower) tower.RestoreCooldown(d.towerCooldown);
            if (b is ScienceLab science) { science.RestoreState(d.scienceRemaining, d.scienceAircraft, d.scienceConstructing, d.scienceSpawn.ToVector3());
                science.RestoreAssignment(d.scienceTier, d.scientist >= 0 ? commanders[d.scientist] : null); }
            if (b is AirshipYard yard) yard.RestoreState(d.airship, commanders);
            if (b is Infirmary infirmary) foreach (var id in d.patients) infirmary.RestorePatient(commanders[id]);
            b.GetComponent<ScoutPost>()?.RestoreState(d.scoutDispatched, d.scoutRemaining, d.scoutDispatchedAnts, d.scoutSuccess, d.scoutFailure);
            b.GetComponent<PrisonerCamp>()?.RestoreState(d.prisoners.Select(p => new Prisoner(p.name, (CommanderRank)p.rank,
                p.roles.Select(r => (UnitRole)r).ToArray(), SaveCatalog.Traits(p.traits)) { PersuadeAttempts = p.persuadeAttempts, Talents = p.talents.Copy() }).ToList(),
                d.prisonEscapeTimer, d.prisonRecruited, d.prisonExecuted, d.prisonEscaped);
            b.GetComponent<NurseryChamber>()?.RestoreState(d.nurseryBirths,
                d.nurseryAffinity.Select(a => commanders[a.firstCommanderId]).ToList(),
                d.nurseryAffinity.Select(a => commanders[a.secondCommanderId]).ToList(), d.nurseryAffinity.Select(a => a.value).ToList());
            var nodes = b.GetComponentsInChildren<ResourceNode>(true);
            foreach (var node in d.nodes) nodes[node.index].RestoreState(node.amount, node.regrowTimer);
        }
    }
}

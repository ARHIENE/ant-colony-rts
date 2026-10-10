using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
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
            d.repairCredit = b.GetComponent<BuildingRepair>()?.Credit ?? 0;
            if (b is Kitchen kitchen) d.kitchen = JsonUtility.FromJson<Kitchen.State>(JsonUtility.ToJson(kitchen.Meals));
            if (b is Decoration decoration) d.decorationQuality = decoration.Quality;
            if (b is Barracks barracks) { d.role = (int)barracks.Role; d.barracksTier = barracks.CurrentTier; d.barracksUpgradeRemaining = barracks.UpgradeRemaining; }
            if (b is ResearchLab lab) { d.role = (int)lab.Role; d.labResearchRemaining = lab.ResearchRemaining;
                d.labResearchCommanderId = commanders.IndexOf(lab.Target); d.labResearchAttack = lab.ResearchIsAttack; }
            if (b is DigSite dig) d.digExpanded = dig.IsExpanded;
            if (b is Gate gate) d.gateOpen = gate.Open;
            if (b is PowerNode power) d.powerCharge = power.Charge;
            d.materialTier = b.MaterialTier; d.mainMaterial = (int)b.MainMaterial;
            if (b is AcidTower tower) d.towerCooldown = tower.Cooldown;
            if (b is AreaAcidTower areaTower) d.towerCooldown = areaTower.Cooldown;
            if (b is TrapPit trap) { d.trapArmed = trap.Armed; d.trapBroken = trap.BrokenSeconds; d.trapRepair = trap.RepairProgress; d.trapRepairPaid = trap.RepairPaid; d.trapSpikeHits = trap.SpikeHits; }
            var plot = b.GetComponent<FarmPlot>();
            if (plot != null) { d.crop = (int)plot.Crop; d.farmWide = plot.Wide; }
            if (b is ScienceLab science) { d.scienceRemaining = science.Remaining; d.scienceAircraft = science.Aircraft;
                d.scienceConstructing = science.Constructing; d.scienceSpawn = new Vec3Dto(science.SpawnPosition);
                d.scienceTier = science.Tier; d.scientist = commanders.IndexOf(science.Target); }
            if (b is AirshipYard yard) d.airship = yard.CaptureState(commanders);
            if (b is Workshop workshop) d.workshop = workshop.CaptureState(commanders);
            if (b is Processor processor) d.processor = processor.CaptureState();
            if (b is Ranch ranch) d.ranch = ranch.CaptureState();
            if (b is RanchFacility facility) d.ranchFacility = facility.CaptureState();
            if (b is Infirmary infirmary) d.patients = infirmary.Patients.Select(c => commanders.IndexOf(c)).ToList();
            var scout = b.GetComponent<ScoutPost>();
            if (scout != null) { d.scoutRemaining = scout.Remaining; d.scoutDispatched = scout.IsDispatched;
                d.scoutDispatchedAnts = scout.DispatchedAnts; d.scoutSuccess = scout.SuccessCount; d.scoutFailure = scout.FailureCount;
                d.scoutCompanion = scout.Companion != null ? commanders.IndexOf(scout.Companion) : -1; }
            var prison = b.GetComponent<PrisonerCamp>();
            if (prison != null) { d.prisonEscapeTimer = prison.EscapeTimer; d.prisonRecruited = prison.RecruitedCount;
                d.prisonExecuted = prison.ExecutedCount; d.prisonEscaped = prison.EscapedCount;
                foreach (var p in prison.Prisoners) d.prisoners.Add(new PrisonerDto { name = p.Name, rank = (int)p.Rank,
                    roles = p.Roles.Select(r => (int)r).ToList(), traits = SaveCatalog.Traits(p.Traits), talents = p.Talents.Copy(), persuadeAttempts = p.PersuadeAttempts,
                    personalState = JsonUtility.FromJson<CommanderPersonalState>(JsonUtility.ToJson(p.PersonalState)), labAttack = p.LabAttack, labArmor = p.LabArmor,
                    strikeCooldown = p.StrikeCooldown, stanceCooldown = p.StanceCooldown }); }
            var nursery = b.GetComponent<NurseryChamber>();
            if (nursery != null) { d.nurseryBirths = nursery.BirthCount;
                var first = new List<CommanderAnt>(); var second = new List<CommanderAnt>(); var values = new List<float>();
                nursery.CaptureAffinity(first, second, values);
                for (var i = 0; i < values.Count; i++) d.nurseryAffinity.Add(new AffinityDto {
                    firstCommanderId = commanders.IndexOf(first[i]), secondCommanderId = commanders.IndexOf(second[i]), value = values[i] }); }
            var nodes = b.GetComponentsInChildren<ResourceNode>(true);
            for (var i = 0; i < nodes.Length; i++) d.nodes.Add(new ResourceNodeDto { index = i, looseCargo = nodes[i].IsLooseCargo, amount = nodes[i].AmountRemaining, regrowTimer = nodes[i].RegrowTimeRemaining, sowRemaining = nodes[i].SowRemaining, gatheringForbidden = nodes[i].GatheringForbidden, bountifulHarvest = nodes[i].BountifulHarvest });
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
            BuildingRepair.For(b).Credit = d.repairCredit;
            if (b is Kitchen kitchen) kitchen.Meals = JsonUtility.FromJson<Kitchen.State>(JsonUtility.ToJson(d.kitchen));
            if (b is Decoration decoration) decoration.Quality = d.decorationQuality;
            if (b is Barracks barracks) barracks.RestoreState(d.barracksTier, d.barracksUpgradeRemaining);
            if (b is ResearchLab lab && d.labResearchCommanderId >= 0) lab.RestoreState(commanders[d.labResearchCommanderId], d.labResearchAttack, d.labResearchRemaining);
            // Phase 4: 여왕방 삭제. 이전 저장에서 진행 중이던 여왕방 낚시 연구는 완료로 처리한다.
            if (b is Stockpile && d.queenFishingRemaining > 0 && GameManager.Instance != null) GameManager.Instance.FishingUnlocked = true;
            if (b is DigSite dig) dig.RestoreExpanded(d.digExpanded);
            if (b is Gate gate) gate.SetOpen(d.gateOpen);
            if (b is PowerNode power) power.Charge = Mathf.Clamp(d.powerCharge, 0, GameBalance.BatteryCapacity);
            b.MaterialTier = Mathf.Max(0, d.materialTier);
            b.MainMaterial = (ResourceType)d.mainMaterial;
            if (b is AcidTower tower) tower.RestoreCooldown(d.towerCooldown);
            if (b is AreaAcidTower areaTower) areaTower.RestoreCooldown(d.towerCooldown);
            if (b is TrapPit trap) trap.RestoreState(d.trapArmed, d.trapBroken, d.trapRepair, d.trapRepairPaid, d.trapSpikeHits);
            if (d.kind == "Farm" && d.runtimeBuilt)
                (b.GetComponent<FarmPlot>() ?? b.gameObject.AddComponent<FarmPlot>()).Configure((FarmCrop)d.crop, d.farmWide);
            if (b is ScienceLab science) { science.RestoreState(d.scienceRemaining, d.scienceAircraft, d.scienceConstructing, d.scienceSpawn.ToVector3());
                science.RestoreAssignment(d.scienceTier, d.scientist >= 0 ? commanders[d.scientist] : null); }
            if (b is AirshipYard yard) yard.RestoreState(d.airship, commanders);
            if (b is Workshop workshop) workshop.RestoreState(d.workshop, commanders);
            if (b is Processor processor) processor.RestoreState(d.processor);
            if (b is Ranch ranch) ranch.RestoreState(d.ranch);
            if (b is RanchFacility facility) facility.RestoreState(d.ranchFacility);
            if (b is Infirmary infirmary) foreach (var id in d.patients) infirmary.RestorePatient(commanders[id]);
            b.GetComponent<ScoutPost>()?.RestoreState(d.scoutDispatched, d.scoutRemaining, d.scoutDispatchedAnts, d.scoutSuccess, d.scoutFailure,
                d.scoutCompanion >= 0 ? commanders[d.scoutCompanion] : null);
            b.GetComponent<PrisonerCamp>()?.RestoreState(d.prisoners.Select(p => new Prisoner(p.name, (CommanderRank)p.rank,
                p.roles.Select(r => (UnitRole)r).ToArray(), SaveCatalog.Traits(p.traits)) { PersuadeAttempts = p.persuadeAttempts, Talents = p.talents.Copy(),
                    PersonalState = JsonUtility.FromJson<CommanderPersonalState>(JsonUtility.ToJson(p.personalState)), LabAttack = p.labAttack, LabArmor = p.labArmor,
                    StrikeCooldown = p.strikeCooldown, StanceCooldown = p.stanceCooldown }).ToList(),
                d.prisonEscapeTimer, d.prisonRecruited, d.prisonExecuted, d.prisonEscaped);
            b.GetComponent<NurseryChamber>()?.RestoreState(d.nurseryBirths,
                d.nurseryAffinity.Select(a => commanders[a.firstCommanderId]).ToList(),
                d.nurseryAffinity.Select(a => commanders[a.secondCommanderId]).ToList(), d.nurseryAffinity.Select(a => a.value).ToList());
            var nodes = b.GetComponentsInChildren<ResourceNode>(true);
            foreach (var node in d.nodes) { nodes[node.index].RestoreState(node.amount, node.regrowTimer, node.sowRemaining); nodes[node.index].IsLooseCargo = node.looseCargo; nodes[node.index].BountifulHarvest = node.bountifulHarvest; nodes[node.index].GatheringForbidden = node.gatheringForbidden; }
        }
    }
}

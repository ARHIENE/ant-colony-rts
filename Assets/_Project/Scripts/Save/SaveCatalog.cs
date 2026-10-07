using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Boss;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AntColony.Save
{
    // 씬 시작 때만 번호를 부여한다. 파괴된 개체의 자리도 남겨 불러오기에서 되살리지 않는다.
    public static class SaveCatalog
    {
        internal static BuildingBase[] Buildings;
        internal static ResourceNode[] Nodes;
        internal static WildMonster[] Monsters;
        internal static BossHealth[] Bosses;
        internal static int[] ColonySizes;
        public static bool Ready => Buildings != null;
        internal static T[] Ordered<T>() where T : Component => Object.FindObjectsByType<T>(FindObjectsSortMode.None)
            .OrderBy(x => Path(x.transform), StringComparer.Ordinal).ToArray();
        private static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name + ":" + t.GetSiblingIndex();
        public static void Initialize()
        {
            Buildings = Ordered<BuildingBase>().Where(b => b.CountsTowardPlayerDefeat && !(b is ExpeditionTransport)).ToArray();
            Nodes = Ordered<ResourceNode>().Where(n => n.GetComponentInParent<BuildingBase>() == null
                || !n.GetComponentInParent<BuildingBase>().CountsTowardPlayerDefeat).ToArray();
            Monsters = Ordered<WildMonster>();
            Bosses = Ordered<BossHealth>();
            ColonySizes = WorldMapManager.Instance.Sites.Select(s => s.Colony != null ? s.Colony.Buildings.Length : 0).ToArray();
        }
        internal static string Kind(BuildingBase b)
        {
            if (b.GetComponent<NurseryChamber>() != null) return "Nursery";
            if (b.GetComponent<ScoutPost>() != null) return "ScoutPost";
            if (b.GetComponent<PrisonerCamp>() != null) return "PrisonerCamp";
            if (b.GetComponent<ResourceNode>() != null) return "Farm";
            if (b is Decoration || b is Housing || b is Wall || b is Door || b is Gate || b is HygieneFixture || b is PowerNode || b is FloorTile || b is RecreationSpot // 모닥불·도박장·책장·목욕통은 종류 이름으로(2026-10-05 수정: 타입 이름 "RecreationSpot"은 건물 종류가 아님)
                || b.Data != null && (b.Data.kind == BuildingKind.Hearth || b.Data.kind == BuildingKind.SleepingMat || b.Data.kind == BuildingKind.DoubleBed || b.Data.kind == BuildingKind.SingleBed || b.Data.kind == BuildingKind.FoodStore || b.Data.kind == BuildingKind.Jar
                    || b.Data.kind == BuildingKind.BunkBed || b.Data.kind == BuildingKind.Hammock || b.Data.kind == BuildingKind.BigTable || b.Data.kind == BuildingKind.BanquetTable || b.Data.kind == BuildingKind.SpikeTrap)) return b.Data.kind.ToString(); // 화덕·자리·큰침대는 종류 이름으로(식당·숙소는 기존대로 타입 이름)
            if (b is Stockpile) return "QueenChamber"; // 저장 호환: 구 여왕방 자리
            return b.GetType().Name;
        }
        internal static int SiteIndex(ExpeditionSite site) => site == null ? -1 : WorldMapManager.Instance.Sites.ToList().IndexOf(site);
        internal static int ShipIndex(ExpeditionTransport ship) => ship == null ? -1 : WorldMapManager.Instance.Transports.ToList().IndexOf(ship);
        internal static TraitsDto Traits(CommanderTraits t) => new TraitsDto { personality = (int)t.Personality,
            values = new List<CommanderTrait>(t.values), passions = t.passions.Select(p => new CommanderPassion { activity = p.activity, flame = p.flame }).ToList() };
        internal static CommanderTraits Traits(TraitsDto t) => new CommanderTraits((CommanderPersonality)t.personality)
            { values = new List<CommanderTrait>(t.values), passions = t.passions.Select(p => new CommanderPassion { activity = p.activity, flame = p.flame }).ToList() };

        public static bool CanSave(out string error)
        {
            error = null;
            if (!Ready || Core.CommanderRoster.Instance == null) error = "Game is still initializing.";
            else if (Object.FindFirstObjectByType<BuildingConstructionSite>() != null) error = "Finish construction before saving.";
            else if (Core.CommanderRoster.Instance.Commanders.Any(c => c.IsConstructing || (c.IsAwayFromHome && (c.IsWorking || c.IsCarrying))
                || (!c.CanResumeDutyAfterLoad && !c.IsHostile && c.PersonalState.rageRemaining <= 0 && !c.Social.diving && !c.IsEmbarked && c.Agent.enabled && c.Agent.isOnNavMesh && (c.Agent.pathPending || c.Agent.hasPath))))
                error = "Stop commanders and finish carrying/building before saving.";
            else if (Object.FindObjectsByType<WildMonster>(FindObjectsSortMode.None).Any(m => m.InCombat
                || (!Monsters.Contains(m) && m.GetComponent<EventActor>() == null && string.IsNullOrEmpty(m.RebelId) && m.GetComponentInParent<ExpeditionSite>()?.Disposition != ConquestDisposition.Lost)))
                error = "Finish the current battle or invasion before saving.";
            else if (WorldMapManager.Instance.Sites.Any(s => s.Defense != null && s.Defense.UnderAttack))
                error = "Finish settlement defense before saving.";
            else if (WorldMapManager.Instance.Transports.Any(s => s != null && s.Route.IsRunning && s.State == ExpeditionState.Deployed))
                error = "Auto transport will save after departing the collection site.";
            return error == null;
        }
    }
}


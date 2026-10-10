using System.Linq;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Buildings
{
    // 철거·가구 이동(2026-10-08 기획): 둘 다 건설 예정지처럼 장수가 현장에서 작업해야 끝난다(작업표 '건설', 대상 우선순위·노란 경보 적용).
    // 철거는 건설비의 70%를 반환하고, 이동은 추가 비용 없이 새 위치로 옮긴다. 방 가구·장식만 이동하고 나머지는 철거 후 다시 짓는다.
    public static class Demolition
    {
        public static bool Pending(BuildingBase b) => Object.FindObjectsByType<DemolitionSite>(FindObjectsSortMode.None).Any(s => s.Target == b);
        private static bool Orderable(BuildingBase b) => b != null && !b.IsDead && b.Data != null && b.CountsTowardPlayerDefeat && !(b is Stockpile) && !Pending(b);
        // 포로가 있는 수용소는 철거하지 않는다(포로 명단이 수용소에만 있다). 이동은 포로가 그대로 남으므로 허용.
        public static bool HoldsPrisoners(BuildingBase b) => b != null && (b.TryGetComponent<PrisonerCamp>(out var camp) && camp.Count > 0 ); // 우리는 철거해도 생물이 그 자리에 남는다(2026-10-11)
        public static bool CanDemolish(BuildingBase b) => Orderable(b) && !HoldsPrisoners(b);
        // 무료 이동은 방 가구(RoomSystem.KindOf)·장식·전력 가구만. 주거·벽·문·바닥·기둥·전선·방어 시설은 철거 후 다시 짓는다(2026-10-10).
        public static bool CanMove(BuildingBase b) => Orderable(b) && b.Data.kind != BuildingKind.Pillar && b.Data.kind != BuildingKind.PowerWire
            && (RoomSystem.KindOf(b) != RoomKind.None || b is Decoration || b is PowerNode);

        public static BuildingConstructionSite OrderDemolish(BuildingBase b)
        {
            if (!CanDemolish(b)) return null;
            return Site(b, b.Position, false);
        }

        // 새 위치 검증은 배치 컨트롤러(BeginMove)가 한다.
        public static BuildingConstructionSite OrderMove(BuildingBase b, Vector3 to)
        {
            if (!CanMove(b)) return null;
            return Site(b, to, true);
        }

        // 주재료 선택(2026-10-10): 재료 고유 종류인 벽·문·성벽·성문, 주거(재개발로 교체), 밭, 비축더미, 전선, 재료 비용이 없는 건물은 고르지 않는다.
        public static bool MaterialSelectable(BuildingKind kind) => !(kind is BuildingKind.SoilWall or BuildingKind.LeafWall or BuildingKind.CapWall or BuildingKind.CastleWall
            or BuildingKind.Door or BuildingKind.LockedDoor or BuildingKind.BarredDoor or BuildingKind.Gate or BuildingKind.Hut or BuildingKind.House or BuildingKind.Apartment
            or BuildingKind.Farm or BuildingKind.MushroomFarm or BuildingKind.AphidPen or BuildingKind.QueenChamber or BuildingKind.PowerWire);
        public static bool CanRenovate(BuildingBase b) => Orderable(b) && MaterialSelectable(b.Data.kind) && b.Data.soilCost > 0 && b.enabled;

        // 재료 개보수: 지시할 때 새 재료를 차감하고 장수가 현장 작업을 마치면 교체한다. 작업 중 건물은 사용 중지.
        // 취소하면 새 재료 100% 반환·기존 상태 복구, 완료하면 교체된 기존 재료 70%를 반환(창고가 차면 건물 주변 바닥).
        public static BuildingConstructionSite OrderRenovate(BuildingBase b, ResourceType material)
        {
            if (!CanRenovate(b) || material == b.MainMaterial || !MaterialInfo.For(material).structural || ResourceManager.Instance == null
                || !ResourceManager.Instance.TrySpend(material, b.Data.soilCost, ResourceReason.Construction)) return null;
            var site = Site(b, b.Position, false, true);
            site.SetRefund(0, b.Data.soilCost, 0, material);
            var mark = site.GetComponent<DemolitionSite>(); mark.NewMaterial = material; mark.WasEnabled = b.enabled; b.enabled = false;
            return site;
        }

        private static BuildingConstructionSite Site(BuildingBase b, Vector3 at, bool move, bool renovate = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = b.name + (renovate ? " 개보수 예정지" : move ? " 이동 예정지" : " 철거 예정지");
            var size = b.GetComponent<Renderer>() is Renderer r ? r.bounds.size : Vector3.one * 2f;
            go.transform.position = new Vector3(at.x, b.Position.y - size.y * .5f + .1f, at.z);
            go.transform.localScale = new Vector3(size.x, .2f, size.z);
            go.GetComponent<Collider>().isTrigger = true;
            go.GetComponent<Renderer>().material.color = renovate ? new Color(.85f, .75f, .3f) : move ? new Color(.3f, .6f, .9f) : new Color(.85f, .3f, .25f);
            var site = go.AddComponent<BuildingConstructionSite>();
            site.Initialize(null, Mathf.Max(1f, b.Data.buildTimeSeconds * (renovate ? GameBalance.RenovateWorkShare : move ? GameBalance.MoveWorkShare : GameBalance.DemolishWorkShare)));
            var mark = go.AddComponent<DemolitionSite>(); mark.Target = b; mark.Move = move; mark.MoveTo = at; mark.Renovate = renovate;
            return site;
        }
    }

    // 철거·이동 예정지 표식. 대상이 사라지면 예정지도 없앤다. 건설 현장처럼 저장 전에 끝내야 한다.
    public sealed class DemolitionSite : MonoBehaviour
    {
        public BuildingBase Target { get; internal set; }
        public bool Move { get; internal set; }
        public Vector3 MoveTo { get; internal set; }
        public bool Renovate { get; internal set; }
        public ResourceType NewMaterial { get; internal set; }
        internal bool WasEnabled { get; set; } = true;

        internal void Finish()
        {
            if (Target == null) return;
            if (Renovate)
            {
                var old = Target.MainMaterial; var ratio = Target.CurrentHealth / Mathf.Max(1f, Target.MaxHealth);
                AntColony.World.DiplomacyManager.StoreResource(old, Mathf.FloorToInt(Target.Data.soilCost * GameBalance.DemolishRefundShare), ResourceReason.Refund, Target.Position);
                Target.MainMaterial = NewMaterial; Target.RestoreHealth(Target.MaxHealth * ratio); Target.enabled = WasEnabled;
                AntColony.UI.ToastManager.Show($"{Target.Data.displayName} 재료 변경 완료: {old.DisplayName()} → {NewMaterial.DisplayName()}");
            }
            else if (Move)
            {
                Target.transform.position = new Vector3(MoveTo.x, Target.Position.y, MoveTo.z);
                AntColony.UI.ToastManager.Show(Target.Data.displayName + " 이동 완료");
            }
            else if (Demolition.HoldsPrisoners(Target))
            {
                AntColony.UI.ToastManager.Show(Target.Data.displayName + " 철거 취소 — 포로·생물을 먼저 비우세요.");
            }
            else
            {
                (Target as Processor)?.RefundCurrent();
                (Target as RanchFacility)?.OnRemoved(); // 먹이통·도축대 내용물은 바닥에, 치료대 환자는 그 자리에
                var d = Target.Data; var share = GameBalance.DemolishRefundShare;
                AntColony.World.DiplomacyManager.StoreResource(ResourceType.Food, Mathf.FloorToInt(d.foodCost * share), ResourceReason.Refund);
                AntColony.World.DiplomacyManager.StoreResource(Target.MainMaterial, Mathf.FloorToInt(d.soilCost * share), ResourceReason.Refund);
                AntColony.World.DiplomacyManager.StoreResource(ResourceType.Special, Mathf.FloorToInt(d.specialCost * share), ResourceReason.Refund);
                AntColony.UI.ToastManager.Show(d.displayName + " 철거 완료");
                Target.gameObject.SetActive(false); Destroy(Target.gameObject);
            }
            RoomSystem.MarkDirty(); Target = null;
        }

        // 개보수 취소: 기존 재료·기능 그대로 사용 재개(새 재료 반환은 건설 예정지 취소가 한다).
        internal void Abort() { if (Renovate && Target != null) Target.enabled = WasEnabled; Target = null; }
        private void OnDestroy() { if (Renovate && Target != null && !Target.IsDead) Target.enabled = WasEnabled; }
        private void Update() { if (Target == null || Target.IsDead) Destroy(gameObject); }
    }
}

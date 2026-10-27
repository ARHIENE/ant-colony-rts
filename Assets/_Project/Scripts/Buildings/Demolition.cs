using System.Linq;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Buildings
{
    // 철거·가구 이동·재료 개보수(2026-10-08 기획, 2026-10-12 갱신): 건설 예정지처럼 장수가 현장에서 작업해야 끝난다(작업표 '건설', 대상 우선순위·노란 경보 적용).
    // 철거: 실제 시작 때 사용 중지·내용물 바닥·진행 중 생산 취소(소비 재료 반환 없음), 완료 시 건설 재료 70%를 바닥에, 완료 전 취소는 기능 복구.
    // 이동(가구·장식만): 회수·포장 → 새 위치 설치, 추가 비용 없음. 회수 후 취소하면 포장된 채 원래 자리 바닥에 남는다.
    // 개보수: 새 재료 예약·배송 → 시작 때 소비·사용 중지(주문·진행도 보존) → 완료 시 기존 재료 70% 바닥, 취소 시 새 재료 100% 반환·기능 복구.
    public static class Demolition
    {
        public static bool Pending(BuildingBase b) => AntColony.Core.SceneObjects.All<DemolitionSite>().Any(s => s.Target == b);
        private static bool Orderable(BuildingBase b) => b != null && !b.IsDead && b.Data != null && b.CountsTowardPlayerDefeat && !(b is Stockpile)
            && !(b is AirshipYard yard && yard.ModificationsLocked)
            && !(b is SupplyFacility source && source.IsNaturalSource) && !Pending(b);
        // 환자·포로·치료 중인 동물이 들어 있는 시설은 비우기 전 철거·이동·개보수를 할 수 없다(2026-10-12).
        public static bool HoldsPrisoners(BuildingBase b) => b != null && (b.TryGetComponent<PrisonerCamp>(out var camp) && camp.Count > 0
            || b is Infirmary ward && ward.Patients.Count > 0
            || b is RanchFacility clinic && clinic.IsClinic && clinic.Patient != null); // 우리는 철거해도 생물이 그 자리에 남는다(2026-10-11)
        // 사용 중인 침대·좌석·휴게·위생 가구는 사용하던 장수가 나올 때까지 작업을 시작하지 않는다.
        public static bool InUse(BuildingBase b) => b != null && AntColony.Units.AntUnitBase.Active.OfType<AntColony.Units.CommanderAnt>().Any(c => !c.IsDead
            && (c.IsAsleep && Dormitory.Of(c) == b || c.PlaySpot == b || c.WashSpot == b || c.IsEating && b is Kitchen && (c.Position - b.Position).sqrMagnitude < 49));
        public static bool IsPacked(BuildingBase b) => b != null && b.TryGetComponent<PackedFurniture>(out _);
        public static bool CanDemolish(BuildingBase b) => Orderable(b) && !HoldsPrisoners(b) && !(b is Tombstone t && t.RemovalBlock != "");
        // 무료 이동은 방 가구(RoomSystem.KindOf)·장식·전력 가구만. 주거·벽·문·바닥·기둥·전선·방어 시설은 철거 후 다시 짓는다(2026-10-10). 포장된 가구는 다시 설치할 수 있다.
        public static bool CanMove(BuildingBase b) => Orderable(b) && !HoldsPrisoners(b) && (IsPacked(b) || b.Data.kind != BuildingKind.Pillar && b.Data.kind != BuildingKind.PowerWire
            && (RoomSystem.KindOf(b) != RoomKind.None || b is Decoration || b is PowerNode || UndergroundProduction.CanRelocate(b)));

        public static BuildingConstructionSite OrderDemolish(BuildingBase b)
        {
            if (!CanDemolish(b)) return null;
            return Site(b, b.Position, false);
        }

        // 새 위치 검증은 배치 컨트롤러(BeginMove)가 한다. 회수는 원래 자리에서, 이미 포장된 가구는 바로 새 위치에서 설치한다.
        public static BuildingConstructionSite OrderMove(BuildingBase b, Vector3 to)
        {
            if (!CanMove(b)) return null;
            if (UndergroundProduction.IsSolidDrill(b.Data.kind) && UndergroundProduction.PlacementReason(b.Data.kind, to, b) != null) return null;
            var packed = IsPacked(b);
            var site = Site(b, packed ? to : b.Position, true);
            var mark = site.GetComponent<DemolitionSite>(); mark.MoveTo = to;
            if (packed) mark.MarkPacked();
            return site;
        }

        // 주재료 선택(2026-10-10): 재료 고유 종류인 벽·문·성벽·성문, 주거(재개발로 교체), 밭, 비축더미, 전선, 재료 비용이 없는 건물은 고르지 않는다.
        public static bool MaterialSelectable(BuildingKind kind) => !EarlyConstruction.HasFixedCost(kind) && !(kind is BuildingKind.SoilWall or BuildingKind.LeafWall or BuildingKind.CapWall or BuildingKind.CastleWall
            or BuildingKind.Door or BuildingKind.LockedDoor or BuildingKind.BarredDoor or BuildingKind.Gate or BuildingKind.Hut or BuildingKind.House or BuildingKind.Apartment
            or BuildingKind.Farm or BuildingKind.MushroomFarm or BuildingKind.AphidPen or BuildingKind.QueenChamber or BuildingKind.PowerWire);
        public static bool CanRenovate(BuildingBase b) => Orderable(b) && !HoldsPrisoners(b) && !IsPacked(b) && MaterialSelectable(b.Data.kind) && b.Data.soilCost > 0 && b.enabled;

        public static BuildingConstructionSite OrderRenovate(BuildingBase b, ResourceType material)
        {
            if (!CanRenovate(b) || material == b.MainMaterial || !MaterialInfo.For(material).structural || ResourceManager.Instance == null) return null;
            var site = Site(b, b.Position, false, true);
            site.Require(material, b.Data.soilCost); // 새 재료는 예약·배송 후 시작할 때 소비. 사용 중지도 시작할 때.
            site.GetComponent<DemolitionSite>().NewMaterial = material;
            return site;
        }

        internal static float MoveHalfWork(BuildingBase b) => Mathf.Max(1f, b.Data.buildTimeSeconds * GameBalance.MoveWorkShare * .5f);
        private static BuildingConstructionSite Site(BuildingBase b, Vector3 at, bool move, bool renovate = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = b.name + (renovate ? " 개보수 예정지" : move ? " 이동 예정지" : " 철거 예정지");
            var size = b.TryGetComponent<Renderer>(out var r) ? r.bounds.size : Vector3.one * 2f;
            go.transform.position = new Vector3(at.x, b.Position.y - size.y * .5f + .1f, at.z);
            go.transform.localScale = new Vector3(size.x, .2f, size.z);
            go.GetComponent<Collider>().isTrigger = true;
            go.GetComponent<Renderer>().material.color = renovate ? new Color(.85f, .75f, .3f) : move ? new Color(.3f, .6f, .9f) : new Color(.85f, .3f, .25f);
            var site = go.AddComponent<BuildingConstructionSite>();
            site.Initialize(null, move ? MoveHalfWork(b) : Mathf.Max(1f, b.Data.buildTimeSeconds * (renovate ? GameBalance.RenovateWorkShare : GameBalance.DemolishWorkShare)));
            var mark = go.AddComponent<DemolitionSite>(); mark.Target = b; mark.Move = move; mark.MoveTo = at; mark.Renovate = renovate;
            return site;
        }

        // 창고·냉장고·먹이통 등 내용물을 원래 자리 바닥에 내놓고, 진행 중 생산을 취소한다(이미 소비한 재료는 반환하지 않는다).
        internal static void EmptyForRemoval(BuildingBase b)
        {
            (b as SupplyFacility)?.EjectStored();
            if (b is Storage store) ResourceManager.Instance?.EjectAll(store.Bin);
            (b as RanchFacility)?.OnRemoved(); // 먹이통·도축대 내용물은 바닥에, 치료대 환자는 그 자리에
            (b as Processor)?.CancelCurrent(); (b as Kitchen)?.CancelCurrent(); (b as Kitchen)?.EjectStaged();
        }
    }

    // 회수·포장된 가구(2026-10-12). 기능은 멈추고 작게 표시된다. 선택해서 다시 설치(이동)할 수 있다.
    public sealed class PackedFurniture : MonoBehaviour
    {
        private Vector3 scale;
        private void OnEnable() => AntColony.Core.SceneObjects.Register(this);
        private void OnDisable() => AntColony.Core.SceneObjects.Unregister(this);
        private void Awake() { scale = transform.localScale; transform.localScale = scale * .5f; }
        internal void Unpack() { transform.localScale = scale; Destroy(this); }
    }

    // 철거·이동·개보수 예정지 표식. 대상이 사라지면 예정지도 없앤다.
    public sealed class DemolitionSite : MonoBehaviour
    {
        private void OnEnable() => SceneObjects.Register(this);
        private void OnDisable() => SceneObjects.Unregister(this);
        public BuildingBase Target { get; internal set; }
        public bool Move { get; internal set; }
        public Vector3 MoveTo { get; internal set; }
        public bool Renovate { get; internal set; }
        public ResourceType NewMaterial { get; internal set; }
        internal bool WasEnabled { get; set; } = true;
        private bool disabled;
        public bool WorkStarted => disabled;
        public bool Packed { get; private set; } // 이동: 회수가 끝나 설치 단계

        // 실제 작업 시작 순간: 사용을 멈춘다. 철거·이동은 내용물을 바닥에 내놓고 진행 중 생산을 취소한다. 개보수는 주문·진행도를 그대로 둔다.
        internal void OnWorkStarted()
        {
            if (Target == null || disabled) return;
            WasEnabled = Target.enabled; disabled = true;
            if (Packed) return; // 이미 포장된 가구는 기능이 꺼져 있다
            if (!Renovate) Demolition.EmptyForRemoval(Target);
            Target.enabled = false;
        }
        internal void MarkPacked() { Packed = true; disabled = true; WasEnabled = true; }

        // 작업 완료. 이동의 회수 단계는 false를 돌려 예정지를 남긴다(새 위치에서 설치 단계).
        internal bool Finish()
        {
            if (Target == null) return true;
            if (Renovate)
            {
                var old = Target.MainMaterial; var ratio = Target.CurrentHealth / Mathf.Max(1f, Target.MaxHealth);
                AntColony.World.DiplomacyManager.DropFloor(old, Mathf.FloorToInt(Target.Data.soilCost * GameBalance.DemolishRefundShare), Target.Position + ResourceManager.Scatter());
                Target.MainMaterial = NewMaterial; Target.RestoreHealth(Target.MaxHealth * ratio); Target.enabled = WasEnabled;
                AntColony.UI.ToastManager.Show($"{Target.Data.displayName} 재료 변경 완료: {old.DisplayName()} → {NewMaterial.DisplayName()}");
            }
            else if (Move && !Packed)
            {
                // 회수·포장 완료 → 새 위치에서 설치. 추가 비용 없음.
                Packed = true; if (!Target.TryGetComponent<PackedFurniture>(out _)) Target.gameObject.AddComponent<PackedFurniture>();
                var site = GetComponent<BuildingConstructionSite>();
                transform.position = new Vector3(MoveTo.x, transform.position.y, MoveTo.z);
                site.RemainingWork = Demolition.MoveHalfWork(Target);
                AntColony.UI.ToastManager.Show(Target.Data.displayName + " 회수·포장 완료 — 새 위치에 설치합니다.");
                return false;
            }
            else if (Move)
            {
                if (UndergroundProduction.IsSolidDrill(Target.Data.kind) && UndergroundProduction.PlacementReason(Target.Data.kind, MoveTo, Target) is string reason)
                {
                    var site = GetComponent<BuildingConstructionSite>(); site.RemainingWork = Demolition.MoveHalfWork(Target); site.SetPaused(true);
                    AntColony.UI.ToastManager.Show("채굴기 설치 대기: " + reason); return false;
                }
                Target.transform.position = new Vector3(MoveTo.x, Target.Position.y, MoveTo.z);
                if (Target.TryGetComponent<PackedFurniture>(out var pack)) pack.Unpack();
                Target.enabled = WasEnabled;
                AntColony.UI.ToastManager.Show(Target.Data.displayName + " 이동 완료");
            }
            else if (Demolition.HoldsPrisoners(Target))
            {
                AntColony.UI.ToastManager.Show(Target.Data.displayName + " 철거 취소 — 환자·포로·생물을 먼저 비우세요.");
            }
            else
            {
                Demolition.EmptyForRemoval(Target); // 시작 뒤 새로 들어온 것까지
                if (Target.TryGetComponent<CrashWreck>(out var wreck)) wreck.Salvage();
                var d = Target.Data; var share = GameBalance.DemolishRefundShare; var at = Target.Position;
                if (EarlyConstruction.HasFixedCost(d.kind))
                    foreach (var cost in EarlyConstruction.Cost(d.kind)) AntColony.World.DiplomacyManager.DropFloor(cost.type, Mathf.FloorToInt(cost.amount * share), at + ResourceManager.Scatter());
                else
                {
                    AntColony.World.DiplomacyManager.DropFloor(ResourceType.Food, Mathf.FloorToInt(d.foodCost * share), at + ResourceManager.Scatter());
                    AntColony.World.DiplomacyManager.DropFloor(Target.MainMaterial, Mathf.FloorToInt(d.soilCost * share), at + ResourceManager.Scatter());
                    AntColony.World.DiplomacyManager.DropFloor(ResourceType.Special, Mathf.FloorToInt(d.specialCost * share), at + ResourceManager.Scatter());
                }
                AntColony.UI.ToastManager.Show(d.displayName + " 철거 완료");
                Target.gameObject.SetActive(false); Destroy(Target.gameObject);
            }
            RoomSystem.MarkDirty(); Target = null;
            return true;
        }

        // 취소: 시작 전·회수 전이면 그대로, 시작 후면 기능 복구(철거 진행도는 초기화). 회수 후 이동 취소는 포장된 채 원래 자리에 남는다.
        internal void Abort()
        {
            if (Target != null && disabled && !Packed) Target.enabled = WasEnabled;
            if (Target != null && Packed && !Target.TryGetComponent<PackedFurniture>(out _)) Target.gameObject.AddComponent<PackedFurniture>();
            Target = null;
        }
        private void OnDestroy() { if (Target != null && disabled && !Packed && !Target.IsDead) Target.enabled = WasEnabled; }
        private void Update() { if (Target == null || Target.IsDead) Destroy(gameObject); }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AntColony.Core;
using AntColony.Data;

namespace AntColony.Buildings
{
    // 건설 예정지(2026-10-12 기획): 자원이 부족해도 배치한다. 가능한 자원만 예약 → 운반 장수(또는 건설 장수)가 창고에서 가져와 배송 →
    // 전부 도착하면 실제 시공을 시작하며 그때 소비한다. 우선순위 1~9(같으면 먼저 지시한 곳)로 예약하고, 순위를 바꿔도 이미 예약·배송한 몫은 빼앗지 않는다.
    public partial class BuildingConstructionSite : MonoBehaviour, IMaterialSink
    {
        [Serializable] public sealed class Need { public ResourceType type; public int need, reserved, delivered; }
        public enum Wait { None, Paused, MaterialShortage, AwaitingHaul, NoWorker, Unreachable, InUse, Forbidden, Tool }

        private static long orderSeq;
        private GameObject completedBuilding;
        private AntPool workforcePool;
        private int reservedAnts;
        private bool finished;
        private readonly Dictionary<object, (ResourceType type, int amount)> inTransit = new Dictionary<object, (ResourceType, int)>(); // 배송 중(장수가 들고 오는) 수량

        public List<Need> Needs { get; } = new List<Need>();
        public bool Started { get; internal set; }
        public bool Paused { get; private set; }
        public long Order { get; internal set; } = ++orderSeq;
        public GameObject Building => completedBuilding;
        public AntColony.Data.BuildingKind? BuildingKind => completedBuilding != null ? completedBuilding.GetComponent<BuildingBase>()?.Data?.kind : null;
        public bool IsArt => BuildingKind.HasValue && Decoration.IsKind(BuildingKind.Value);
        public bool HasBuilder => Builders.Any();
        public IEnumerable<AntColony.Units.CommanderAnt> Builders => AntColony.Units.AntUnitBase.Active.OfType<AntColony.Units.CommanderAnt>().Where(w => w.ConstructionTarget == this);
        public IEnumerable<AntColony.Units.CommanderAnt> Couriers => AntColony.Units.AntUnitBase.Active.OfType<AntColony.Units.CommanderAnt>().Where(w => w.DeliverySite == this);
        public float BuildTimeSeconds { get; private set; }
        public float RemainingWork { get; internal set; }
        public Vector3 Position => transform.position;
        // 모든 재료가 도착해야 시공한다. 재료가 없는 예정지(철거·이동)는 바로 시공한다.
        public bool Ready => Needs.All(n => n.delivered >= n.need);
        public bool CanWork => !finished && !Paused && Ready && !WaitingForUser;
        // 철거·이동·개보수 대상 침대·좌석을 누가 쓰고 있으면 나올 때까지 시작하지 않는다.
        public bool WaitingForUser => !Started && TryGetComponent<DemolitionSite>(out var m) && Demolition.InUse(m.Target);
        public int InTransit(ResourceType t) => inTransit.Values.Where(v => v.type == t).Sum(v => v.amount);
        public int Delivered(ResourceType t) => Needs.Where(n => n.type == t).Sum(n => n.delivered);
        // 배송을 기다리는(예약했지만 아직 아무도 집지 않은) 재료.
        public Need PendingPickup => Paused || finished ? null : Needs.FirstOrDefault(n => n.reserved > 0);
        public bool NextPickup(out ResourceType type, out int amount)
        { var n = PendingPickup; type = n?.type ?? default; amount = n?.reserved ?? 0; return n != null; }

        public void Initialize(GameObject building, float buildTimeSeconds, AntPool pool = null, int workforce = 0)
        {
            completedBuilding = building;
            BuildTimeSeconds = buildTimeSeconds;
            RemainingWork = buildTimeSeconds;
            workforcePool = pool;
            reservedAnts = workforce;
        }
        private void Start()
        {
            var box = GetComponent<Renderer>();
            if (box == null) return;
            var bounds = box.bounds;
            var demolition = GetComponent<DemolitionSite>();
            var id = demolition != null && !demolition.Move && !demolition.Renovate ? "CC_DemolitionRubble" : "CC_ConstructionPlan";
            var model = AntColony.World.ProductionProp.Attach(transform, "ResourceModels/" + id,
                Mathf.Max(bounds.size.x, bounds.size.z), new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            if (model != null) box.forceRenderingOff = true;
        }
        // 필요 재료를 등록한다(0은 건너뜀). 같은 종류는 합친다.
        public void Require(ResourceType type, int amount)
        {
            if (amount <= 0) return;
            var n = Needs.FirstOrDefault(x => x.type == type);
            if (n == null) Needs.Add(new Need { type = type, need = amount }); else n.need += amount;
        }

        // 예약 분배: 우선순위 높은 예정지 → 먼저 지시한 곳. 이미 예약·배송한 몫은 빼앗지 않는다. 창고 재고가 예약보다 줄면(다른 소비) 낮은 순위부터 예약을 줄인다.
        private static int allocatedFrame = -1;
        public static void Allocate()
        {
            if (allocatedFrame == Time.frameCount) return; allocatedFrame = Time.frameCount;
            var rm = ResourceManager.Instance; if (rm == null) return;
            var sites = AntColony.Core.SceneObjects.All<BuildingConstructionSite>().Where(s => !s.finished && !s.Started)
                .OrderByDescending(s => AntColony.Units.WorkPriorities.Level(s)).ThenBy(s => s.Order).ToList();
            foreach (var site in sites) site.ReevaluateReservations();
            foreach (var type in sites.SelectMany(s => s.Needs).Select(n => n.type).Distinct().ToList())
            {
                var stock = rm.GetAmount(type);
                var reserved = sites.SelectMany(s => s.Needs).Where(n => n.type == type).Sum(n => n.reserved);
                for (int i = sites.Count - 1; i >= 0 && reserved > stock; i--)
                    foreach (var n in sites[i].Needs.Where(n => n.type == type)) { var cut = Mathf.Min(n.reserved, reserved - stock); n.reserved -= cut; reserved -= cut; }
                var free = stock - reserved;
                foreach (var s in sites)
                {
                    if (free <= 0) break;
                    if (!s.HasAvailableCrew) continue;
                    foreach (var n in s.Needs.Where(n => n.type == type))
                    {
                        var want = n.need - n.delivered - n.reserved - s.InTransit(type);
                        var give = Mathf.Clamp(want, 0, free); n.reserved += give; free -= give;
                    }
                }
            }
        }
        private bool HasAvailableCrew => !Paused && !finished && AntColony.Units.AntUnitBase.Active.OfType<AntColony.Units.CommanderAnt>()
            .Any(c => c.CivilianWorkReady && !c.IsCaptive && !c.IsEmbarked && (c.AllowsJob(Job) || !Ready && c.AllowsJob(AntColony.Units.CommanderJobs.Hauling))
                && c.TryWorkApproach(Position, out _));
        internal void ReevaluateReservations()
        {
            if (!HasAvailableCrew) ReleaseUnusedReservations();
        }
        internal void ReleaseUnusedReservations()
        {
            foreach (var n in Needs) n.reserved = 0;
            ReturnWorkforce(); allocatedFrame = -1;
        }
        private void Update()
        {
            if (AntColony.Save.SaveSystem.Busy) return;
            foreach (var courier in inTransit.Keys.OfType<AntColony.Units.WorkerAnt>().ToArray())
                if (courier == null || !courier.isActiveAndEnabled || courier.DeliverySite != this) inTransit.Remove(courier);
            ReevaluateReservations(); Allocate();
        }

        // 장수가 창고에서 실제로 집은 만큼 예약 → 배송 중으로 옮긴다.
        public bool BeginPickup(object courier, ResourceType type, int amount)
        {
            var n = Needs.FirstOrDefault(x => x.type == type);
            if (n == null || amount <= 0 || Paused || finished) return false;
            n.reserved = Mathf.Max(0, n.reserved - amount);
            inTransit[courier] = (type, amount);
            return true;
        }
        // 배송 도착: 필요한 만큼만 받는다. 받은 양을 돌려준다(남는 짐은 장수가 창고로 되가져간다).
        public int Deliver(object courier, ResourceType type, int amount)
        {
            AbortPickup(courier);
            var n = Needs.FirstOrDefault(x => x.type == type);
            if (n == null || finished || Paused) return 0;
            var take = Mathf.Clamp(n.need - n.delivered, 0, amount); n.delivered += take;
            if (take > 0 && Ready) AntColony.UI.ToastManager.Show(name + ": 재료가 모두 도착했습니다.");
            return take;
        }
        public void AbortPickup(object courier) { inTransit.Remove(courier); }

        // 실제 시공 시작 순간 재료를 소비한다(기록). 시작 후에는 재료를 바꿀 수 없다.
        public void BeginWork()
        {
            if (Started || !Ready) return;
            Started = true;
            if (TryGetComponent<DemolitionSite>(out var mark)) mark.OnWorkStarted();
            foreach (var n in Needs) CampaignHistory.Resource(n.type, n.delivered, true, ResourceReason.Construction);
        }

        // 일시정지: 새 배송·시공을 멈춘다. 도착 재료·진행도는 보존하고 아직 집지 않은 예약은 푼다.
        public void SetPaused(bool on)
        {
            if (finished || Paused == on) return;
            Paused = on;
            if (on) { ReleaseUnusedReservations(); foreach (var b in Builders.Concat(Couriers).Distinct().ToList()) b.CommandStop(); }
        }
        // 시공 시작 전 재료 변경: 기존 예약 해제·도착분 바닥 배출 후 새 재료를 요청한다.
        public bool ChangeMaterial(ResourceType material)
        {
            var building = completedBuilding != null ? completedBuilding.GetComponent<BuildingBase>() : null;
            var n = Needs.FirstOrDefault(x => x.type == building?.MainMaterial);
            if (Started || finished || building == null || n == null || material == n.type || !material.IsMaterial()) return false;
            DropOnFloor(n.type, n.delivered);
            foreach (var c in Couriers.ToList()) c.CommandStop();
            var amount = n.need; Needs.Remove(n); Require(material, amount);
            building.MainMaterial = material;
            return true;
        }

        public void Complete(AntColony.Units.CommanderAnt builder = null)
        {
            if (finished) return;
            if (TryGetComponent<DemolitionSite>(out var demolition) && !demolition.Finish()) return; // 이동: 회수 끝, 새 위치에서 설치 단계로
            finished = true;
            ReturnWorkforce();
            if (completedBuilding != null)
            {
                if (builder != null && completedBuilding.GetComponent<Decoration>() is Decoration decor)
                    decor.Quality = AntColony.Units.EquipmentRecipes.Quality(builder.Talents.Level(AntColony.Units.CommanderActivity.Art), UnityEngine.Random.value, UnityEngine.Random.value);
                CompleteAttribution(completedBuilding.GetComponent<BuildingBase>());
                if (TryGetComponent<RedevelopmentSite>(out var redevelop)) redevelop.Finish(); // 재개발이면 완공 순간 기존 집을 허문다.
                completedBuilding.SetActive(true);
                AntColony.UI.ToastManager.Show(completedBuilding.name + " construction complete.");
                completedBuilding = null;
            }
            Destroy(gameObject);
        }

        // 플레이어 취소(2026-10-12): 시공 전이면 예약 해제·도착한 재료를 바닥에, 시공 후면 재료 100%를 바닥에 돌려준다.
        // 재개발 보상비는 배치할 때 지급했으므로 돌려주지 않는다. 배송 중인 장수는 짐을 창고로 되가져간다.
        public void Cancel()
        {
            if (finished) return;
            finished = true;
            ReleaseUnusedReservations();
            foreach (var c in Builders.Concat(Couriers).Distinct().ToList()) c.CommandStop();
            foreach (var n in Needs) DropOnFloor(n.type, n.delivered);
            if (TryGetComponent<DemolitionSite>(out var marker)) marker.Abort(); // 개보수 취소: 건물 사용 재개
            ReturnWorkforce();
            Destroy(gameObject);
        }
        private void DropOnFloor(ResourceType t, int amount) => AntColony.World.DiplomacyManager.DropFloor(t, amount, Position + ResourceManager.Scatter());

        // 대기 사유(작업 지시 UI): 일시정지 · 재료 부족 · 운반 대기 · 담당 장수 없음 · 접근 불가. 시공·배송 중이면 None.
        public Wait WaitReason
        {
            get
            {
                if (Paused) return Wait.Paused;
                if (WaitingForUser) return Wait.InUse;
                var crew = AntColony.Units.AntUnitBase.Active.OfType<AntColony.Units.CommanderAnt>().Where(c => c.IsColonyMember && !c.IsDead).ToList();
                if (!Ready)
                {
                    if (Couriers.Any()) return Wait.None;
                    if (crew.Count == 0) return Wait.NoWorker;
                    var haulers = crew.Where(c => c.AllowsJob(AntColony.Units.CommanderJobs.Hauling) || c.AllowsJob(Job)).ToArray();
                    if (haulers.Length == 0) return Wait.Forbidden;
                    if (!haulers.Any(c => c.CivilianWorkReady && c.TryWorkApproach(Position, out _))) return Wait.Unreachable;
                    if (Needs.Any(n => n.reserved > 0)) return crew.Any(c => c.AllowsJob(AntColony.Units.CommanderJobs.Hauling) || c.AllowsJob(Job)) ? Wait.AwaitingHaul : Wait.NoWorker;
                    return Wait.MaterialShortage;
                }
                if (HasBuilder) return Wait.None;
                var able = crew.Where(c => c.AllowsJob(Job)).ToList();
                if (crew.Count == 0) return Wait.NoWorker;
                if (able.Count == 0) return Wait.Forbidden;
                return able.Any(c => c.TryWorkApproach(Position, out _)) ? Wait.None : Wait.Unreachable;
            }
        }
        public AntColony.Units.CommanderJobs Job => IsArt ? AntColony.Units.CommanderJobs.Art : AntColony.Units.CommanderJobs.Building;
        public static string Describe(Wait w) => w switch
        {
            Wait.Paused => "일시정지", Wait.MaterialShortage => "재료 부족", Wait.AwaitingHaul => "운반 대기",
            Wait.NoWorker => "담당 없음", Wait.Forbidden => "작업 금지", Wait.Tool => "도구 없음", Wait.Unreachable => "경로 없음", Wait.InUse => "사용 중(나올 때까지 대기)", _ => "진행 중"
        };

        private void ReturnWorkforce()
        {
            var count = reservedAnts;
            reservedAnts = 0;
            if (workforcePool != null) workforcePool.ReleaseReserved(count);
        }

        private void OnDestroy()
        {
            ReleaseUnusedReservations();
            foreach (var c in Builders.Concat(Couriers).Distinct().ToList()) c.CommandStop();
            ReturnWorkforce();
            if (completedBuilding != null)
            {
                Destroy(completedBuilding);
            }
        }

        private void OnEnable() => AntColony.Core.SceneObjects.Register(this);
        private void OnDisable()
        {
            AntColony.Core.SceneObjects.Unregister(this);
            ReleaseUnusedReservations();
            foreach (var c in Builders.Concat(Couriers).Distinct().ToList()) c.CommandStop();
        }
    }
}

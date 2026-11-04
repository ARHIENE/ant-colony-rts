using System.Linq;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.World
{
    // 편입 거점 1~3곳을 지정 순서로 경유하는 왕복(2026-10-25 D09). 이동·채집·반납은 기존 원정 명령을 사용한다.
    public sealed class TransportRoute
    {
        public const int MaxStops = 3;
        // 재출발 간격: 기본 60초, 수송 효율 연구 후 30초.
        public static float IntervalSeconds => ScienceEffects.RouteIntervalSeconds;
        private readonly ExpeditionTransport ship;
        private ResourceNode[] nodes;
        private int[] nextNode;
        private bool rallying;
        private ExpeditionSite[] stops;
        private int leg;
        public System.Collections.Generic.IReadOnlyList<ExpeditionSite> Stops => stops ?? System.Array.Empty<ExpeditionSite>();
        // 지금 향하거나 머무는 경유지. 본거지에서는 첫 경유지.
        public ExpeditionSite Destination { get; private set; }
        public bool IsRunning { get; private set; }
        public float WaitSeconds { get; private set; }
        public string Status { get; private set; } = "Off";

        public TransportRoute(ExpeditionTransport transport) => ship = transport;

        public bool TryStart(ExpeditionSite destination) => TryStart(new[] { destination });

        public bool TryStart(System.Collections.Generic.IReadOnlyList<ExpeditionSite> route)
        {
            if (IsRunning || !ship.isActiveAndEnabled || ship.State != ExpeditionState.Home
                || WorldMapManager.Instance == null || !WorldMapManager.Instance.Unlocked
                || route == null || route.Count < 1 || route.Count > MaxStops || route.Distinct().Count() != route.Count
                || !route.All(Valid) || !HasCollectors()) return false;
            stops = route.ToArray();
            leg = 0;
            Destination = stops[0];
            WaitSeconds = 0;
            nextNode = null;
            rallying = false;
            IsRunning = true;
            Status = "Ready";
            return true;
        }

        // 저장 복원 전용. 현재 왕복 상태(목적지·대기시간)를 그대로 되돌린다.
        internal void RestoreState(bool running, ExpeditionSite destination, float wait, string status)
        {
            IsRunning = running && destination != null;
            Destination = destination;
            WaitSeconds = Mathf.Max(0f, wait);
            Status = string.IsNullOrEmpty(status) ? (IsRunning ? "Ready" : "Off") : status;
            nextNode = null;
            rallying = false;
        }

        private void FitCursor() { if (nextNode != null && nextNode.Length != ship.Crew.Count) System.Array.Resize(ref nextNode, ship.Crew.Count); }

        internal void CaptureProgress(Save.RouteDto dto)
        {
            FitCursor(); // 출발 뒤 승무원이 바뀌면 커서 길이도 맞춘다(저장 검증: 길이 = 승무원 수).
            dto.rallying = rallying; dto.nextNode = nextNode == null ? null : (int[])nextNode.Clone();
            dto.nodes = nextNode == null ? null : nodes?.Select(n => Save.SaveReferences.Key(n)).ToList();
            dto.stops = stops?.Select(Save.SaveCatalog.SiteIndex).ToList(); dto.leg = leg;
        }
        internal void RestoreProgress(Save.RouteDto dto)
        {
            rallying = dto.rallying; nextNode = dto.nextNode == null || dto.nextNode.Length == 0 ? null : (int[])dto.nextNode.Clone(); // JsonUtility: null → []
            nodes = dto.nodes?.Select(n => Save.SaveReferences.Get<ResourceNode>(n)).ToArray();
            // 구저장(단일 거점)은 목적지 하나를 경유지로 본다.
            var sites = WorldMapManager.Instance.Sites;
            stops = dto.stops != null && dto.stops.Count > 0 && dto.stops.All(i => i >= 0 && i < sites.Count)
                ? dto.stops.Select(i => sites[i]).ToArray() : Destination != null ? new[] { Destination } : null;
            leg = stops == null ? 0 : Mathf.Clamp(dto.leg, 0, stops.Length - 1);
        }

        private static bool Valid(ExpeditionSite s) => s != null && s.isActiveAndEnabled && s.Disposition == ConquestDisposition.Annexed
            && s.Settlement != null && s.Settlement.isActiveAndEnabled;

        public void Stop(string reason = "Off")
        {
            IsRunning = false;
            Status = reason;
            // 현장 작업과 이동은 유지한다. 본거지에서는 다음 출발용 승무원도 내려준다.
            if (ship.isActiveAndEnabled && ship.State == ExpeditionState.Home) ship.TryUnloadCrew();
        }

        private bool HasCollectors()
        {
            var found = false;
            foreach (var c in ship.Crew)
            {
                if (c == null || !c.isActiveAndEnabled || c.Transport != ship || !c.HasTroops && !ship.HasBoardedTroopsFor(c)) return false;
                if (c.Data.gatherRate > 0 && c.Data.carryCapacity > 0) found = true;
            }
            return found;
        }

        public void Tick(float seconds)
        {
            if (!IsRunning || !(seconds > 0) || float.IsInfinity(seconds)) return;
            if (ship.FieldWorkActive) { Status = "현장 수리 진행 중"; return; }
            if (ship.State == ExpeditionState.Home)
            {
                // 잃은 경유지는 본거지에서 빼고 남은 순서로 계속한다(다음 사이클부터 반영).
                var kept = stops?.Where(Valid).ToArray();
                if (kept != null && kept.Length != stops.Length && kept.Length > 0)
                    AntColony.UI.ToastManager.Show($"자동 수송: 잃은 경유지 {stops.Length - kept.Length}곳을 제외했습니다.", AntColony.UI.ToastKind.Warning);
                stops = kept; leg = 0;
                Destination = stops != null && stops.Length > 0 ? stops[0] : null;
            }
            if (!Valid(Destination) || !HasCollectors()) { Stop("Stopped: check site and worker crew"); return; }

            if (ship.State == ExpeditionState.Home)
            {
                if (ship.PendingCrewOrderCount > 0) { Status = ship.CrewOrderStatus; return; }
                // HasCargo에는 핵심물도 포함한다. 반입 전에는 다음 왕복을 시작하지 않는다.
                if (ship.HasCargo) { Status = "Waiting for home storage"; return; }
                WaitSeconds = Mathf.Max(0, WaitSeconds - seconds);
                if (WaitSeconds > 0) { Status = $"Next trip in {WaitSeconds:0}s"; return; }
                if (!ship.TryDepart(Destination)) { Status = "Waiting for destination"; return; }
                nextNode = null;
                rallying = false;
            }
            if (ship.State != ExpeditionState.Deployed) { Status = ship.State.ToString(); return; }
            if (ship.IsWithdrawing || ship.ReservedCargo > 0) { Status = "철수 / 적재 진행 중"; return; }
            if (ship.Site != Destination) { Stop("Stopped: destination changed"); return; }
            if (nextNode == null)
            {
                nodes = Destination.Colony.GetComponentsInChildren<ResourceNode>(true);
                nextNode = new int[ship.Crew.Count];
            }
            FitCursor();

            var finished = true;
            for (var i = 0; i < ship.Crew.Count; i++)
            {
                var c = ship.Crew[i];
                if (c.IsWorking || c.IsCarrying) { finished = false; continue; }
                if (rallying || ship.CargoFull) continue;
                while (nextNode[i] < nodes.Length)
                {
                    // 한 짐에 다 못 실은 더미는 빌 때까지 다시 캔다(운반 한도 < 더미 양일 때 남기고 떠나지 않게).
                    var node = nodes[nextNode[i]];
                    if (node == null || !node.CanGather) { nextNode[i]++; continue; }
                    if (!c.CanReach(node.transform.position)) { Stop("Stopped: resource unreachable"); return; }
                    c.CommandGather(node);
                    if (!c.IsWorking && !c.IsCarrying) { nextNode[i]++; continue; } // 이 장수가 캘 수 없는 자원은 건너뛴다
                    finished = false;
                    break;
                }
            }
            if (!finished) { Status = "Collecting / depositing"; return; }
            if (!rallying)
            {
                foreach (var c in ship.Crew) c.CommandMove(ship.Position + Vector3.right * 3);
                rallying = true;
            }
            Status = "Waiting for crew / garrison cargo";
            // 화물 여유가 남으면 다음 경유지로, 아니면 귀환한다.
            int nextLeg = leg + 1;
            while (nextLeg < stops.Length && !Valid(stops[nextLeg]))
            {
                AntColony.UI.ToastManager.Show("자동 수송: 잃은 경유지를 제외합니다.", AntColony.UI.ToastKind.Warning);
                nextLeg++;
            }
            if (nextLeg < stops.Length && ship.ReturnCargoSpace >= 1)
            {
                if (!ship.TryContinueTo(stops[nextLeg])) return;
                leg = nextLeg;
                Destination = stops[leg];
                nextNode = null;
                rallying = false;
                Status = $"Moving to stop {leg + 1}";
                return;
            }
            if (ship.TryReturn())
            {
                WaitSeconds = IntervalSeconds;
                Status = "Returning";
            }
        }
    }
}

using AntColony.Data;
using UnityEngine;

namespace AntColony.World
{
    // 단일 거점 왕복만 담당한다. 이동·채집·반납은 기존 원정 명령을 사용한다.
    public sealed class TransportRoute
    {
        // ponytail: 재출발 간격은 임시값. 밸런스 확정 시 거점 생산 주기와 함께 조정한다.
        public const float IntervalSeconds = 60f;
        private readonly ExpeditionTransport ship;
        private ResourceNode[] nodes;
        private int[] nextNode;
        private bool rallying;
        public ExpeditionSite Destination { get; private set; }
        public bool IsRunning { get; private set; }
        public float WaitSeconds { get; private set; }
        public string Status { get; private set; } = "Off";

        public TransportRoute(ExpeditionTransport transport) => ship = transport;

        public bool TryStart(ExpeditionSite destination)
        {
            if (IsRunning || !ship.isActiveAndEnabled || ship.State != ExpeditionState.Home
                || WorldMapManager.Instance == null || !WorldMapManager.Instance.Unlocked
                || destination == null || !destination.isActiveAndEnabled
                || destination.Disposition != ConquestDisposition.Annexed
                || destination.Settlement == null || !destination.Settlement.isActiveAndEnabled
                || !HasCollectors()) return false;
            Destination = destination;
            WaitSeconds = 0;
            nextNode = null;
            rallying = false;
            IsRunning = true;
            Status = "Ready";
            return true;
        }

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
                if (c == null || !c.isActiveAndEnabled || c.Transport != ship || !c.HasTroops) return false;
                if (c.Role == UnitRole.Worker && c.Data.gatherRate > 0 && c.Data.carryCapacity > 0) found = true;
            }
            return found;
        }

        public void Tick(float seconds)
        {
            if (!IsRunning || !(seconds > 0) || float.IsInfinity(seconds)) return;
            if (Destination == null || !Destination.isActiveAndEnabled
                || Destination.Disposition != ConquestDisposition.Annexed
                || Destination.Settlement == null || !Destination.Settlement.isActiveAndEnabled
                || !HasCollectors()) { Stop("Stopped: check site and worker crew"); return; }

            if (ship.State == ExpeditionState.Home)
            {
                if (ship.HasCargo) { Status = "Waiting for home storage"; return; }
                WaitSeconds = Mathf.Max(0, WaitSeconds - seconds);
                if (WaitSeconds > 0) { Status = $"Next trip in {WaitSeconds:0}s"; return; }
                if (!ship.TryDepart(Destination)) { Status = "Waiting for destination"; return; }
                nextNode = null;
                rallying = false;
            }
            if (ship.State != ExpeditionState.Deployed) { Status = ship.State.ToString(); return; }
            if (ship.Site != Destination) { Stop("Stopped: destination changed"); return; }
            if (nextNode == null)
            {
                nodes = Destination.Colony.GetComponentsInChildren<ResourceNode>(true);
                nextNode = new int[ship.Crew.Count];
            }

            var finished = true;
            for (var i = 0; i < ship.Crew.Count; i++)
            {
                var c = ship.Crew[i];
                if (c.IsWorking || c.IsCarrying) { finished = false; continue; }
                if (rallying || c.Role != UnitRole.Worker) continue;
                while (nextNode[i] < nodes.Length)
                {
                    var node = nodes[nextNode[i]++];
                    if (node == null || !node.CanGather) continue;
                    if (!c.CanReach(node.transform.position)) { Stop("Stopped: resource unreachable"); return; }
                    c.CommandGather(node);
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
            if (ship.TryReturn())
            {
                WaitSeconds = IntervalSeconds;
                Status = "Returning";
            }
        }
    }
}

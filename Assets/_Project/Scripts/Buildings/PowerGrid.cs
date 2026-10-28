using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AntColony.Buildings
{
    // 전력망 계산(2026-10-05): 칸이 맞닿은 전력 가구끼리 한 망. 0.5초마다 발전량 → 소비 → 남으면 배터리 충전, 모자라면 배터리 방전.
    // 부족하면 건물의 전력 우선순위가 높은 부하부터 공급한다. 공급할 수 없는 부하는 진행을 보존한다.
    public sealed class PowerGrid : MonoBehaviour
    {
        public const float TickSeconds = .5f;
        private static PowerGrid instance;
        private static bool dirty = true;
        private static readonly Dictionary<PowerNode, int> networkOf = new Dictionary<PowerNode, int>();
        private static readonly List<List<PowerNode>> networks = new List<List<PowerNode>>();
        private float timer;

        public static void MarkDirty() { dirty = true; Ensure(); }
        // 전력 가구가 아닌 소비 건물(냉장고·냉동고 등, 2026-10-12). 칸이 맞닿은 망에서 전력을 받는다.
        public interface IUser { IEnumerable<Vector2Int> Cells { get; } float Demand { get; } bool Powered { set; } }
        private static readonly List<IUser> users = new List<IUser>();
        private static readonly Dictionary<IUser, int> userNet = new Dictionary<IUser, int>();
        public static void Register(IUser u) { if (!users.Contains(u)) users.Add(u); MarkDirty(); }
        public static void Unregister(IUser u) { users.Remove(u); Invalidate(); }
        // 비활성·파괴 경로용: 씬 종료 OnDisable 중 망 오브젝트를 새로 만들지 않는다(다음 사용 때 Ensure).
        public static void Invalidate() => dirty = true;
        private static void Ensure()
        {
            if (instance != null || !Application.isPlaying) return;
            instance = new GameObject("PowerGrid").AddComponent<PowerGrid>();
        }
        private void OnDestroy() { if (instance == this) instance = null; }

        public static IReadOnlyList<PowerNode> NetworkOf(PowerNode node) { Rebuild(); return networkOf.TryGetValue(node, out var i) ? networks[i] : new List<PowerNode> { node }; }
        // 배터리가 있는 망은 배터리가 덜 찼을 때, 없는 망은 소비 가구가 있을 때 발전기가 일한다.
        public static bool WantsPower(PowerNode node)
        {
            var net = NetworkOf(node);
            var index = networkOf.TryGetValue(node, out var i) ? i : -1;
            return net.Any(n => n.IsConsumer || n.IsBattery && n.Charge < Core.GameBalance.BatteryCapacity - .01f)
                || users.Any(u => userNet.TryGetValue(u, out var j) && j == index && u.Demand > 0);
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < TickSeconds) return;
            Tick(timer); timer = 0;
        }

        public static void Tick(float seconds)
        {
            Rebuild();
            foreach (var net in networks)
            {
                var supply = net.Sum(n => n.Output) * seconds;
                var netIndex = networks.IndexOf(net);
                var netUsers = users.Where(u => userNet.TryGetValue(u, out var i) && i == netIndex).ToList();
                var batteries = net.Where(n => n.IsBattery).ToList();
                var stored = batteries.Sum(b => b.Charge);
                var available = supply + stored; float used = 0;
                var loads = net.Where(n => n.IsConsumer).Select(n => (Node: n, User: (IUser)null, Priority: n.PowerPriority))
                    .Concat(netUsers.Select(u => (Node: (PowerNode)null, User: u, Priority: (u as BuildingBase)?.PowerPriority ?? 5)))
                    .OrderByDescending(l => l.Priority);
                foreach (var load in loads)
                {
                    var energy = (load.Node != null ? load.Node.Demand : load.User.Demand) * seconds;
                    var powered = energy > 0 && available >= energy;
                    if (powered) { available -= energy; used += energy; }
                    if (load.Node != null) load.Node.Powered = powered;
                    else load.User.Powered = powered;
                }
                var surplus = supply - used; // 실제 공급한 부하만 소비한다.
                foreach (var b in batteries)
                {
                    // 남으면 차례로 충전, 모자라면 차례로 방전.
                    var change = surplus >= 0 ? Mathf.Min(surplus, Core.GameBalance.BatteryCapacity - b.Charge) : -Mathf.Min(-surplus, b.Charge);
                    b.Charge += change; surplus -= change;
                }
            }
            foreach (var u in users) if (!userNet.ContainsKey(u)) u.Powered = false; // 망에 닿지 않으면 정전
        }

        private static void Rebuild()
        {
            if (!dirty) return;
            dirty = false; networks.Clear(); networkOf.Clear();
            var cellOf = new Dictionary<Vector2Int, List<PowerNode>>();
            var nodes = PowerNode.All.Where(n => n != null && !n.IsDead).ToList();
            var cells = nodes.ToDictionary(n => n, n => RoomSystem.Footprint(n));
            foreach (var n in nodes) foreach (var c in cells[n])
                { if (!cellOf.TryGetValue(c, out var list)) cellOf[c] = list = new List<PowerNode>(); list.Add(n); }
            foreach (var start in nodes)
            {
                if (networkOf.ContainsKey(start)) continue;
                var net = new List<PowerNode>(); var queue = new Queue<PowerNode>(); queue.Enqueue(start); networkOf[start] = networks.Count;
                while (queue.Count > 0)
                {
                    var n = queue.Dequeue(); net.Add(n);
                    foreach (var c in cells[n])
                        foreach (var d in new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                            if (cellOf.TryGetValue(c + d, out var near))
                                foreach (var m in near) if (!networkOf.ContainsKey(m)) { networkOf[m] = networks.Count; queue.Enqueue(m); }
                }
                networks.Add(net);
            }
            userNet.Clear();
            foreach (var u in users)
                foreach (var c in u.Cells)
                    foreach (var d in new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                        if (!userNet.ContainsKey(u) && cellOf.TryGetValue(c + d, out var near) && near.Count > 0) userNet[u] = networkOf[near[0]];
        }
    }
}

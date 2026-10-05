using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AntColony.Buildings
{
    // 전력망 계산(2026-10-05): 칸이 맞닿은 전력 가구끼리 한 망. 0.5초마다 발전량 → 소비 → 남으면 배터리 충전, 모자라면 배터리 방전.
    // 모자란 망은 소비 가구 전체가 꺼진다(산소미포함처럼 순서 차단은 아직 없음).
    public sealed class PowerGrid : MonoBehaviour
    {
        public const float TickSeconds = .5f;
        private static PowerGrid instance;
        private static bool dirty = true;
        private static readonly Dictionary<PowerNode, int> networkOf = new Dictionary<PowerNode, int>();
        private static readonly List<List<PowerNode>> networks = new List<List<PowerNode>>();
        private float timer;

        public static void MarkDirty() { dirty = true; Ensure(); }
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
            return net.Any(n => n.IsBattery) ? net.Any(n => n.IsBattery && n.Charge < Core.GameBalance.BatteryCapacity - .01f) : net.Any(n => n.IsConsumer);
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
                var demand = net.Sum(n => n.Demand) * seconds;
                var batteries = net.Where(n => n.IsBattery).ToList();
                var stored = batteries.Sum(b => b.Charge);
                var powered = supply + stored >= demand && demand > 0;
                var surplus = supply - (powered ? demand : 0); // 꺼진 망은 소비하지 않는다
                foreach (var b in batteries)
                {
                    // 남으면 차례로 충전, 모자라면 차례로 방전.
                    var change = surplus >= 0 ? Mathf.Min(surplus, Core.GameBalance.BatteryCapacity - b.Charge) : -Mathf.Min(-surplus, b.Charge);
                    b.Charge += change; surplus -= change;
                }
                foreach (var n in net) if (n.IsConsumer) n.Powered = powered;
            }
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
        }
    }
}

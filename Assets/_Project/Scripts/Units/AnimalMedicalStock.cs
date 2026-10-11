using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;

namespace AntColony.Units
{
    // 동물용 의약품·수술 키트 예약(2026-10-11): 작업을 맡는 순간 1개를 예약해 같은 재고를 두 작업이 함께 쓰지 못하게 한다.
    // 실제 소비(투약 완료·수술 시작) 때 예약을 풀고 차감한다. 작업이 시작 전에 멈추면 예약만 해제한다. 저장하지 않는다(작업도 불러오면 다시 잡는다).
    public static class AnimalMedicalStock
    {
        private static readonly Dictionary<CommanderAnt, ResourceType> Held = new Dictionary<CommanderAnt, ResourceType>();

        public static int Reserved(ResourceType t) { Prune(); return Held.Values.Count(v => v == t); }
        public static int Available(ResourceType t) => ResourceManager.Instance == null ? 0 : ResourceManager.Instance.GetAmount(t) - Reserved(t);
        public static bool Holds(CommanderAnt c, ResourceType t) => c != null && Held.TryGetValue(c, out var v) && v == t;

        public static bool Reserve(CommanderAnt c, ResourceType t)
        {
            if (c == null) return false;
            if (Holds(c, t)) return true;
            Release(c);
            if (Available(t) < 1) return false;
            Held[c] = t; return true;
        }
        public static void Release(CommanderAnt c) { if (c != null) Held.Remove(c); }
        // 예약분을 실제로 쓴다. 예약 없이 부르면 남은 재고가 있을 때만 쓴다.
        public static bool Consume(CommanderAnt c, ResourceType t)
        {
            var held = Holds(c, t); Release(c);
            if (!held && Available(t) < 1) return false;
            return ResourceManager.Instance != null && ResourceManager.Instance.TrySpend(t, 1, ResourceReason.Upkeep);
        }
        private static void Prune() { foreach (var c in Held.Keys.Where(k => k == null).ToList()) Held.Remove(c); }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Core
{
    public enum FoodPreservation { RawMeat, Fish, Cooked, Berry, Mushroom, Grain, Nut, StartingRation }

    // 식품 신선도(2026-10-12): 물량마다 남은 보존 시간(게임 초)을 따로 갖는다. 옮기거나 묶어도 원래 신선도를 유지한다.
    // 꺼낼 때는 남은 시간이 짧은 것부터(썩기 전 식품 중 신선도 낮은 것부터 먹는다). 시간이 다 되면 썩은 식품이 된다.
    [Serializable] public sealed class FoodLots
    {
        [Serializable] public struct Lot { public int amount; public float shelf; }
        public List<Lot> lots = new List<Lot>();

        public static float ShelfLife(FoodPreservation food) => GameCalendar.SecondsPerDay * (food switch
        {
            FoodPreservation.RawMeat or FoodPreservation.Fish or FoodPreservation.Cooked => 3f,
            FoodPreservation.Berry or FoodPreservation.Mushroom => 6f,
            _ => 24f
        });

        // Food는 원산지가 없는 통합 자원이다. 원산지를 아는 생산자는 종류별 기간으로 Lot을 생성한다.
        public static float ShelfLife(ResourceType t) => t switch
        {
            ResourceType.Food => ShelfLife(FoodPreservation.RawMeat),
            ResourceType.Honeydew => GameCalendar.SecondsPerMonth,
            ResourceType.SimpleMeal => ShelfLife(FoodPreservation.Cooked),
            ResourceType.FineMeal => ShelfLife(FoodPreservation.Cooked),
            _ => 0f
        };
        public static bool Perishable(ResourceType t) => ShelfLife(t) > 0;

        public int Total => lots.Sum(l => l.amount);
        public float Freshest => lots.Count == 0 ? 0 : lots.Max(l => l.shelf);
        public float Stalest => lots.Count == 0 ? 0 : lots.Min(l => l.shelf);
        public void Add(int amount, float shelf)
        {
            if (amount <= 0) return;
            var i = lots.FindIndex(l => Mathf.Abs(l.shelf - shelf) < 1f); // 거의 같은 신선도는 합친다
            if (i >= 0) lots[i] = new Lot { amount = lots[i].amount + amount, shelf = Mathf.Min(lots[i].shelf, shelf) };
            else lots.Add(new Lot { amount = amount, shelf = shelf });
        }
        public void Add(FoodLots other) { if (other != null) foreach (var l in other.lots) Add(l.amount, l.shelf); }
        // 신선도 낮은 것부터 amount만큼 떼어 낸다.
        public FoodLots Take(int amount)
        {
            var taken = new FoodLots();
            lots.Sort((a, b) => a.shelf.CompareTo(b.shelf));
            for (int i = 0; i < lots.Count && amount > 0;)
            {
                var n = Mathf.Min(amount, lots[i].amount); taken.Add(n, lots[i].shelf); amount -= n;
                if (n == lots[i].amount) lots.RemoveAt(i); else { lots[i] = new Lot { amount = lots[i].amount - n, shelf = lots[i].shelf }; i++; }
            }
            return taken;
        }
        // 시간 경과. rate = 1(상온) · 냉장고 느리게 · 냉동고 0. 다 된 물량을 돌려준다(썩은 식품으로 바꿀 몫).
        public int Age(float seconds)
        {
            int spoiled = 0;
            for (int i = lots.Count - 1; i >= 0; i--)
            {
                var l = lots[i]; l.shelf -= seconds;
                if (l.shelf <= 0) { spoiled += l.amount; lots.RemoveAt(i); } else lots[i] = l;
            }
            return spoiled;
        }
        // 총량이 바깥에서 바뀌었을 때(이전 저장·검사 등) 신선한 물량을 더하거나 오래된 것부터 빼서 맞춘다.
        public void Match(int total, ResourceType t)
        {
            var diff = total - Total;
            if (diff > 0) Add(diff, ShelfLife(t)); else if (diff < 0) Take(-diff);
        }
        public bool Valid => lots != null && lots.All(l => l.amount > 0 && !float.IsNaN(l.shelf) && l.shelf > 0);
    }
}

namespace AntColony.Core
{
    // 부패 알림: 종류별 수량을 30초 동안 모아 오른쪽 알림창에 한 번 띄운다(2026-10-12).
    public static class SpoilNotice
    {
        private static readonly System.Collections.Generic.Dictionary<ResourceType, int> pending = new System.Collections.Generic.Dictionary<ResourceType, int>();
        private static float wait;
        public static void Add(ResourceType t, int n) { pending[t] = (pending.TryGetValue(t, out var had) ? had : 0) + n; }
        public static void Tick(float seconds)
        {
            wait += seconds; if (wait < 30f || pending.Count == 0) return;
            AntColony.UI.ToastManager.Show("식품 부패: " + string.Join(" · ", System.Linq.Enumerable.Select(pending, p => $"{p.Key.DisplayName()} {p.Value}")) + " → 썩은 식품");
            pending.Clear(); wait = 0;
        }
    }
}

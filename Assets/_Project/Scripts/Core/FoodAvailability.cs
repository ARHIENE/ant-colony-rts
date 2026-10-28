using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.Map;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.Core
{
    // 식단 창에서만 계산한다. 창고 접근성은 장수별이며 예약을 새로 만들거나 옮기지 않는다.
    public sealed class FoodAvailability
    {
        public sealed class Row
        {
            public ResourceType type;
            public int stored, available, construction, cooking, floor, carried, transport, remote, spoiling;
            public bool HasStock => stored + cooking + floor + carried + transport + remote > 0;
        }
        public readonly List<Row> rows = new List<Row>();
        public CommanderAnt observer;
        public int StoredEdible => rows.Where(r => r.type.IsEdible()).Sum(r => r.stored);
        public int AvailableMeals => observer == null ? 0 : rows.Sum(r => r.available / (CookedFoods.Quality(r.type) > 0 ? 1 : observer.RationPortion));
        public string Summary
        {
            get
            {
                if (observer == null) return "본거지에 확인할 장수가 없습니다.";
                if (WorkPriorities.Red) return "빨간 경보 중 — 식사를 중단합니다. 경보 해제 여부를 확인하세요.";
                if (observer.IsAwayFromHome || observer.IsEmbarked || observer.IsDeployed) return "출전·원정·탑승 중 — 일반 창고 식사와 보급/휴대 식량을 구분하세요.";
                if (observer.IsEating) return "식사 중입니다.";
                if (observer.CarriedRationMeals > 0 && observer.DietAllows(CommanderAnt.DietRaw)) return $"휴대 보존식 {observer.CarriedRationMeals}끼 보유(창고 수량과 별도) · 창고에서 먹을 수 있는 분량 {AvailableMeals}끼";
                if (AvailableMeals > 0) return $"현재 식단·경로로 창고에서 먹을 수 있는 분량 {AvailableMeals}끼";
                if (observer.CarriedRationMeals > 0 && !observer.DietAllows(CommanderAnt.DietRaw)) return "휴대 보존식은 있지만 날것 식단이 금지됐습니다 — 날것 허용 또는 다른 요리 공급을 선택하세요.";
                if (observer.Diet == 0) return "정상 음식 식단이 전부 금지됨 — 아래 식단에서 먹을 종류를 허용하세요.";
                if (StoredEdible > 0) return "정상 음식은 있지만 식단·경로·한 끼 분량 조건을 만족하지 못합니다. 아래 가용 수량과 창고 위치를 확인하세요.";
                if (rows.Any(r => r.type.IsEdible() && r.floor > 0)) return "음식이 바닥에 있습니다 — 운반 작업과 입고 가능한 창고를 확인하세요.";
                if (rows.Any(r => r.type == ResourceType.RottenFood && r.stored > 0)) return "썩은 식품만 남았습니다 — 정상 식품을 공급하세요. 썩은 식품 섭취는 기분 하락·식중독 위험이 있습니다.";
                if (rows.Any(r => r.stored > 0)) return "창고에는 조리 재료/술만 있습니다 — 먹을 수 있는 요리를 생산하세요.";
                return "바로 먹을 음식이 없습니다 — 수확·낚시·요리·보급을 확인하세요.";
            }
        }
        public static int StoredEdibleCount => ResourceManager.Instance == null ? 0 : CookedFoods.MealTypes.Where(t => t.IsEdible()).Sum(ResourceManager.Instance.GetAmount);
        public static FoodAvailability Read(CommanderAnt observer)
        {
            var result = new FoodAvailability { observer = observer }; var rm = ResourceManager.Instance;
            if (rm == null) return result;
            var bounds = HomeMapBuilder.CurrentWorldBounds;
            bool Home(Vector3 p) => bounds.Contains(new Vector3(p.x, 0, p.z));
            var bins = rm.Bins.ToArray();
            var reachable = bins.ToDictionary(b => b, b => Home(b.Position) && b.Spendable && observer != null && !observer.IsAwayFromHome && observer.TryWorkApproach(b.Position, out _));
            var crew = AntUnitBase.Active.OfType<CommanderAnt>().Where(c => c.IsColonyMember && !c.IsDead && c.isActiveAndEnabled && Home(c.Position)).ToArray();
            var floor = ResourceNode.Available.Where(n => n.IsLooseCargo && Home(n.transform.position)).ToArray();
            var sites = SceneObjects.All<BuildingConstructionSite>().Where(s => s.isActiveAndEnabled && !s.Started && Home(s.Position)).ToArray();
            var ingredients = SceneObjects.All<Kitchen>().Where(k => !k.IsDead && k.isActiveAndEnabled && Home(k.Position)).SelectMany(k => k.IngredientStock)
                .Concat(SceneObjects.All<Processor>().Where(p => !p.IsDead && p.isActiveAndEnabled && Home(p.Position)).SelectMany(p => p.IngredientStock)).ToArray();
            var ships = SceneObjects.All<ExpeditionTransport>().Where(t => !t.IsDead).ToArray();
            foreach (ResourceType t in Enum.GetValues(typeof(ResourceType)))
            {
                if (!t.IsFood()) continue;
                var quality = CookedFoods.Quality(t);
                var allowed = observer != null && t.IsEdible() && observer.DietAllows(quality == 2 ? CommanderAnt.DietFine : quality == 1 ? CommanderAnt.DietSimple : CommanderAnt.DietRaw);
                result.rows.Add(new Row {
                    type = t,
                    stored = bins.Where(b => Home(b.Position)).Sum(b => b.Amount(t)),
                    available = allowed ? bins.Where(b => reachable[b]).Sum(b => b.Amount(t)) : 0,
                    construction = sites.SelectMany(s => s.Needs).Where(n => n.type == t).Sum(n => n.reserved),
                    cooking = ingredients.Where(s => s.type == t).Sum(s => s.amount),
                    floor = Mathf.FloorToInt(floor.Where(n => n.ResourceType == t).Sum(n => n.AmountRemaining)),
                    carried = Mathf.FloorToInt(crew.Where(c => c.IsCarrying && c.CarriedType == t).Sum(c => c.CarriedAmount)),
                    transport = ships.Sum(s => s.GetCargo(t)),
                    remote = bins.Where(b => !Home(b.Position)).Sum(b => b.Amount(t)),
                    spoiling = bins.Where(b => Home(b.Position) && b.Amount(t) > 0 && FoodLots.Perishable(t)).Sum(b => b.Lots(t)?.lots.Where(l => l.shelf <= GameCalendar.SecondsPerDay).Sum(l => l.amount) ?? 0)
                });
            }
            return result;
        }
    }
}

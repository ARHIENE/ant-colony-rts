using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using UnityEngine;
using ResourceType = AntColony.Data.ResourceType;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        public Kitchen MealTable { get; private set; }
        public int MealSeatSlot { get; internal set; } = -1;
        internal void RestoreMealSeat(Save.CommanderOrderDto order)
        {
            if(!IsEating) return;
            MealTable=Save.SaveReferences.Get<Kitchen>(order.mealTable);
            MealSeatSlot=order.mealSeat;
            if(MealTable && order.mealExit != null && Agent && Agent.enabled && Agent.isOnNavMesh)
                Agent.Warp(order.mealExit.ToVector3());
        }
        bool? mealNavigationUpdates;
        internal void UpdateMealSeat()
        {
            if(!MealTable || MealTable.IsDead || !MealTable.isActiveAndEnabled) { ResetMealSeat(); return; }
            if(!MealTable || !Agent || !Agent.enabled || !Agent.isOnNavMesh) return;
            var visual=MealTable.GetComponent<FacilityVisual>();
            if(!visual || !visual.TryDiningSeat(this,out var seat,out _)) return;
            if(!mealNavigationUpdates.HasValue) { mealNavigationUpdates=Agent.updatePosition; Agent.updatePosition=false; }
            transform.position=new Vector3(seat.x,Agent.nextPosition.y,seat.z);
        }
        void ResetMealSeat()
        {
            if(mealNavigationUpdates.HasValue && Agent)
            {
                if(Agent.enabled && Agent.isOnNavMesh) Agent.Warp(Agent.nextPosition);
                Agent.updatePosition=mealNavigationUpdates.Value;
            }
            mealNavigationUpdates=null;
            MealTable=null;
            MealSeatSlot=-1;
        }

        private bool TryMealTableApproach(Kitchen table, out Vector3 point)
        {
            point = Position;
            var collider = table.GetComponent<Collider>();
            if (!collider) return false;
            var bounds = collider.bounds;
            var outward = Vector3.ProjectOnPlane(Position-bounds.center, Vector3.up).normalized;
            if (outward.sqrMagnitude < .01f) outward = table.transform.forward;
            for (int i=0; i<12; i++)
            {
                var direction = Quaternion.Euler(0,i*30,0)*outward;
                float edge = Mathf.Min(bounds.extents.x/Mathf.Max(.001f,Mathf.Abs(direction.x)),
                    bounds.extents.z/Mathf.Max(.001f,Mathf.Abs(direction.z)));
                var desired = bounds.center + direction*(edge+.45f);
                desired.y = bounds.min.y;
                if (!UnityEngine.AI.NavMesh.SamplePosition(desired,out var hit,.35f,UnityEngine.AI.NavMesh.AllAreas)
                    || !CanReach(hit.position)) continue;
                point = hit.position; return true;
            }
            return false;
        }

        private bool MoveToMealTable(Kitchen table)
        {
            if (!TryMealTableApproach(table,out var point)) return false;
            MealTable = table;
            if (Vector3.ProjectOnPlane(Position-point,Vector3.up).sqrMagnitude <= .04f) return false;
            if (IsFlying || Agent != null && !Agent.pathPending && !Agent.hasPath) base.CommandMove(point);
            return true;
        }

        // 작업과 같은 완전 경로를 확인하고 실제 접근 지점에 도착한 뒤에만 꺼낸다.
        private bool MoveForMeal(Vector3 source)
        {
            if (!TryWorkApproach(source, out var approach)) return false;
            if ((Position - approach).sqrMagnitude <= 2.25f) return false;
            if (IsFlying || Agent != null && !Agent.pathPending && !Agent.hasPath) base.CommandMove(approach);
            return true;
        }

        private bool FetchMeal(bool walk = true)
        {
            var m = MealState; var rm = ResourceManager.Instance;
            var bins = rm == null ? new StoreBin[0] : rm.Bins.Where(b => b.Spendable && TryWorkApproach(b.Position, out _)).ToArray();
            var kitchens = AntColony.Core.SceneObjects.All<Kitchen>()
                .Where(k => k.BestMeal(Diet) != null && TryWorkApproach(k.Position, out _)).ToArray();
            foreach (var type in AntColony.Data.CookedFoods.MealTypes)
            {
                if (m.pickedFood > 0 && m.pickedType != (int)type) continue;
                var quality = AntColony.Data.CookedFoods.Quality(type);
                var kitchen = m.pickedFood > 0 ? null : kitchens.Where(k => k.BestMeal(Diet).quality >= quality && k.BestMeal(Diet).quality > 0)
                    .OrderByDescending(k => k.BestMeal(Diet).quality).ThenBy(k => (k.Position - Position).sqrMagnitude).FirstOrDefault();
                var allowed = type == ResourceType.RottenFood || DietAllows(quality == 2 ? DietFine : quality == 1 ? DietSimple : DietRaw);
                var need = quality > 0 ? 1 : RationPortion;
                var stocked = FoodLots.MealSources(bins, type).ToArray();
                if (!allowed || stocked.Sum(b => b.Amount(type)) < need - m.pickedFood)
                {
                    if (kitchen == null) continue;
                }
                // 옛 부엌 식사는 lot 정보가 없어 같은 등급의 정상 재고보다 뒤에 둔다.
                if (kitchen != null && (kitchen.BestMeal(Diet).quality > quality || !allowed || stocked.Sum(b => b.Amount(type)) < need - m.pickedFood))
                {
                    if (walk && MoveForMeal(kitchen.Position)) return false;
                    if (!TryWorkApproach(kitchen.Position, out _)) continue;
                    var meal = kitchen.TakeMeal(kitchen.BestMeal(Diet));
                    if (meal == null) continue;
                    m.quality = meal.quality; m.cookSkill = meal.skill; m.inspired = false; m.rotten = false; m.served = true;
                    TakeMealExtra(bins);
                    return true;
                }
                var bin = stocked.FirstOrDefault();
                if (bin == null) continue;
                if (walk && MoveForMeal(bin.Position)) return false;
                if (!bin.Spendable || !TryWorkApproach(bin.Position, out _)) continue;
                m.pickedType = (int)type;
                m.pickedFood += rm.TakeFrom(bin, type, Mathf.Min(need - m.pickedFood, bin.Lots(type)?.StalestAmount ?? need), out var eatenLots);
                if (m.pickedFood < need) return false;
                m.pickedFood = 0; m.quality = quality; m.cookSkill = quality > 0 ? eatenLots?.CookingSkill ?? 5 : 0;
                m.inspired = quality > 0 && eatenLots?.Inspired == true;
                m.rotten = type == ResourceType.RottenFood; m.served = true;
                if (quality > 0) TakeMealExtra(bins);
                return true;
            }
            personalState.AddMood("Hunger", traits.Has(CommanderTrait.Ascetic) ? 5 : -15, GameBalance.StarveRetrySeconds + 1);
            OnHunger(); m.retrySeconds = GameBalance.StarveRetrySeconds;
            return false;
        }

        // 병상 급식 호출도 같은 접근성·식단·신선도 검사를 거친다(환자에게 이동 명령은 내리지 않는다).
        private void StartMeal(Kitchen kitchen)
        {
            for (int i = 0; i < RationPortion && !MealState.served; i++)
                if (!FetchMeal(false) && MealState.retrySeconds > 0) return;
            if (!MealState.served) return;
            MealState.served = false;
            MealState.eatSeconds = GameBalance.EatSeconds * (traits.Has(CommanderTrait.SlowEater) ? 2 : 1);
        }

        private void TakeMealExtra(StoreBin[] bins)
        {
            var extra = Mathf.CeilToInt(GameBalance.MealFood * traits.FoodMultiplier - GameBalance.MealFood);
            foreach (var bin in FoodLots.MealSources(bins, ResourceType.Food))
                if (extra > 0 && TryWorkApproach(bin.Position, out var approach) && (Position - approach).sqrMagnitude <= 2.25f)
                    extra -= ResourceManager.Instance.TakeFrom(bin, ResourceType.Food, extra);
        }
    }
}

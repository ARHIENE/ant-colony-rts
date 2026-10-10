using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.UI;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    // 생물 행동(2026-10-11): 배고프면 식성에 맞는 가장 가까운 접근 가능한 먹이(먹이통·바닥·창고·밭·사체)를 찾아가 먹고,
    // 먹을 게 없는 포식자는 먹잇감을 사냥한다. 배부르면 돌아다닌다(우리 안이면 우리 칸 안에서만).
    // 먹이를 독점 예약하지 않고 실제로 먹은 만큼만 줄어든다. 벽·닫힌 문 너머(다른 방)의 먹이는 접근 불가로 본다.
    public sealed partial class Critter
    {
        private enum FoodKind { Feeder, Floor, Storage, Crop, Carcass }
        private Component foodTarget; private FoodKind foodKind; private Critter prey;
        private float think, eatTimer, attackTimer, fleeUntil;
        private Vector3 wanderTo; private static NavMeshPath path;
        public bool Eating => foodTarget != null;

        private void TickBehaviour(float dt)
        {
            if (agitated > 0)
            {
                if ((agitated -= dt) > 0) return;
                Monster.Calm(); ClearAlert();
            }
            Monster.Docile = true;
            if (Bound || Downed || Clinic != null || Handler != null && CalledTo == null) { Halt(); return; }
            if (fleeUntil > Time.time) return;
            if (CalledTo != null) { if (CalledTo == null || CalledTo.IsDead) CalledTo = null; else { MoveTo(CalledTo.Position); return; } }
            if ((think -= dt) <= 0)
            {
                think = 1.5f;
                if (s.hunger >= GameBalance.CritterFullHunger) { foodTarget = null; prey = null; } // 배부르면 탐색·포식을 멈춘다
                else if (s.hunger < GameBalance.CritterSeekHunger && !ValidFood()) { foodTarget = null; FindFood(); }
                if (foodTarget == null && prey == null) Wander();
            }
            if (prey != null) { Hunt(dt); return; }
            if (foodTarget != null) EatFrom(dt);
        }

        // ---------- 먹이 ----------
        private bool ValidFood() => foodTarget != null && foodTarget.gameObject.activeInHierarchy && Available(foodTarget, foodKind);
        private bool Available(Component c, FoodKind kind) => kind switch
        {
            FoodKind.Feeder => c is RanchFacility f && f.Usable && Info.Feeds.Any(t => f.Stock(t) >= 1),
            FoodKind.Floor => c is ResourceNode n && n.IsLooseCargo && n.CanGather && n.AmountRemaining >= 1 && Info.Eats(n.ResourceType),
            FoodKind.Storage => c is BuildingBase b && !b.IsDead && b.isActiveAndEnabled && ResourceManager.Instance != null && Info.Feeds.Any(t => ResourceManager.Instance.GetAmount(t) >= 1),
            FoodKind.Crop => c is ResourceNode crop && crop.CropEdible,
            FoodKind.Carcass => c is CritterCarcass k && k.Meat > 0 && k.Carrier == null,
            _ => false
        };
        private void FindFood()
        {
            Component best = null; var bestKind = FoodKind.Floor; var bestSqr = float.MaxValue; var here = transform.position;
            void Consider(Component c, FoodKind kind)
            {
                if (c == null) return;
                var d = (c.transform.position - here).sqrMagnitude;
                if (d < bestSqr && Available(c, kind) && Reachable(c.transform.position)) { best = c; bestKind = kind; bestSqr = d; }
            }
            foreach (var f in RanchFacility.All.Where(f => f.IsFeeder)) Consider(f, FoodKind.Feeder);
            foreach (var n in ResourceNode.Available.Where(n => n != null && n.IsLooseCargo)) Consider(n, FoodKind.Floor);
            foreach (var b in Object.FindObjectsByType<BuildingBase>(FindObjectsSortMode.None).Where(b => b is Storage || b is Stockpile)) Consider(b, FoodKind.Storage);
            if (Info.plantDiet) foreach (var n in ResourceNode.Available.Where(n => n != null && n.GetComponent<FarmPlot>() != null)) Consider(n, FoodKind.Crop);
            else foreach (var k in CritterCarcass.All) Consider(k, FoodKind.Carcass);
            foodTarget = best; foodKind = bestKind; eatTimer = 0;
            // 먹을 게 없는 배고픈 포식자는 먹잇감을 사냥한다(합사한 동물도 대상, 사전 경고 없음).
            if (best == null && Info.prey.Length > 0 && s.hunger < GameBalance.CritterHuntHunger)
                prey = All.Where(p => p != this && p != null && Info.prey.Contains(p.Species) && p.Carrier == null && p.Clinic == null && p.Monster != null && !p.Monster.IsDead
                    && (p.transform.position - here).sqrMagnitude < GameBalance.CritterHuntRadius * GameBalance.CritterHuntRadius && Reachable(p.transform.position))
                    .OrderBy(p => (p.transform.position - here).sqrMagnitude).FirstOrDefault();
        }
        private void EatFrom(float dt)
        {
            if (!ValidFood()) { foodTarget = null; return; }
            var reach = foodKind == FoodKind.Feeder || foodKind == FoodKind.Storage || foodKind == FoodKind.Crop ? 2.6f : 1.6f;
            var d = foodTarget.transform.position - transform.position; d.y = 0;
            if (d.magnitude > reach) { MoveTo(foodTarget.transform.position); return; }
            Halt();
            if ((eatTimer += dt) < GameBalance.CritterEatSeconds) return;
            eatTimer = 0;
            var ate = foodKind switch
            {
                FoodKind.Feeder => ((RanchFacility)foodTarget).TakeFood(Info),
                FoodKind.Floor => ((ResourceNode)foodTarget).Extract(1) > 0,
                FoodKind.Storage => Info.Feeds.Any(t => ResourceManager.Instance.GetAmount(t) >= 1 && ResourceManager.Instance.TrySpend(t, 1, ResourceReason.Upkeep)),
                FoodKind.Crop => ((ResourceNode)foodTarget).EatCrop(GameBalance.CritterCropBite),
                FoodKind.Carcass => ((CritterCarcass)foodTarget).Eat(1, this),
                _ => false
            };
            if (ate) Feed(1); else foodTarget = null;
            if (s.hunger >= GameBalance.CritterFullHunger) foodTarget = null;
        }

        // ---------- 포식 ----------
        private void Hunt(float dt)
        {
            if (prey == null || prey.Monster == null || prey.Monster.IsDead || prey.Carrier != null || prey.Clinic != null) { prey = null; think = 0; return; }
            var d = prey.transform.position - transform.position; d.y = 0;
            if (d.magnitude > 1.9f) { MoveTo(prey.transform.position); return; }
            Halt();
            if ((attackTimer -= dt) > 0) return;
            attackTimer = 1.25f;
            prey.HitBy(this, Info.damage);
            if (prey == null || prey.Monster == null || prey.Monster.IsDead) { prey = null; think = 0; }
        }
        // 포식자에게 공격받음: 도주형은 달아나고, 그 외는 반격한다.
        private void HitBy(Critter predator, float damage)
        {
            if (Info.temper == CritterTemper.Flee)
            {
                var away = transform.position + (transform.position - predator.transform.position).normalized * 6;
                if (NavMesh.SamplePosition(away, out var hit, 4, NavMesh.AllAreas) && Reachable(hit.position)) { MoveTo(hit.position); fleeUntil = Time.time + 3; }
            }
            else predator.Wound(Info.damage * .5f, "싸움");
            Wound(damage, "포식", predator);
        }

        // ---------- 이동 ----------
        private void Wander()
        {
            if ((transform.position - wanderTo).sqrMagnitude > 1.5f && Monster.Agent != null && Monster.Agent.hasPath) return;
            if (UnityEngine.Random.value > .4f) return;
            Vector3 to;
            if (InOperatingPen) { var cells = Pen.Room.Cells; var c = cells[UnityEngine.Random.Range(0, cells.Count)]; to = new Vector3(c.x + .5f, transform.position.y, c.y + .5f); }
            else { var home = s.home != null ? s.home.ToVector3() : transform.position; to = home + Random.insideUnitSphere.Flat() * 5; }
            if (NavMesh.SamplePosition(to, out var hit, 2, NavMesh.AllAreas) && Reachable(hit.position)) { wanderTo = hit.position; MoveTo(hit.position); }
        }
        // 같은 공간(같은 방, 또는 둘 다 방 밖)이고 길이 이어져 있어야 접근 가능.
        public bool Reachable(Vector3 to)
        {
            if (RoomSystem.RoomAt(to) != RoomSystem.RoomAt(transform.position)) return false;
            var agent = Monster.Agent; if (agent == null || !agent.enabled || !agent.isOnNavMesh) return false;
            if (!NavMesh.SamplePosition(to, out var hit, 3, NavMesh.AllAreas)) return false;
            path ??= new NavMeshPath();
            return NavMesh.CalculatePath(agent.nextPosition, hit.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
        }
        internal void MoveTo(Vector3 p)
        {
            var agent = Monster.Agent;
            if (agent == null || !agent.enabled || !agent.isOnNavMesh || Monster.RootRemaining > 0) return;
            agent.speed = Monster.MoveSpeed * (Stage == CritterStage.Baby ? .7f : 1f);
            agent.SetDestination(p);
        }
        internal void Halt() { var agent = Monster.Agent; if (agent != null && agent.enabled && agent.isOnNavMesh && agent.hasPath) agent.ResetPath(); }

        // 장수에게 공격받음(사냥·전투): 성향대로 반응한다.
        internal void OnHurtByUnit() { if (!Agitated) Agitate(null); }
    }

    internal static class CritterVectorExtensions { public static Vector3 Flat(this Vector3 v) => new Vector3(v.x, 0, v.z); }
}

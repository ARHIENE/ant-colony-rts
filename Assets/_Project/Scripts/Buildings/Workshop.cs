using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    public sealed class Workshop : BuildingBase
    {
        [Serializable] public class Job { public EquipmentRecipe recipe; public float work; public string makerId = ""; public bool materialsPaid; }
        // 생산 목록(2026-10-08 기획): 지정 수량(amount개 만들고 끝) / 재고 유지(보관함 재고가 amount 미만이면 생산) / 계속 생산.
        // 대기열이 비면 목록 위에서부터 만들 수 있는 품목 1개를 대기열에 넣는다(비용은 그때 차감, 취소 환급은 기존 대기열 규칙).
        public enum OrderMode { Count, KeepStock, Forever }
        [Serializable] public class Order { public EquipmentRecipe recipe; public OrderMode mode; public int amount = 1; }
        [Serializable] public class State
        {
            public List<Job> jobs = new List<Job>();
            public List<Order> orders = new List<Order>();
            public int crafter = -1;
            public bool ruined, paused, inactive;
            public int materialVersion;
        }
        private List<Job> jobs = new List<Job>();
        public IReadOnlyList<Job> Jobs => jobs;
        private List<Order> orders = new List<Order>();
        public List<Order> Orders => orders;
        public const int MaxOrders = 10, MaxOrderAmount = 30;
        public bool AddOrder(EquipmentRecipe recipe, OrderMode mode, int amount)
        {
            if (orders.Count >= MaxOrders || !EquipmentRecipes.Unlocked(recipe)) return false;
            orders.Add(new Order { recipe = recipe, mode = mode, amount = Mathf.Clamp(amount, 1, MaxOrderAmount) }); return true;
        }
        public static int Stock(EquipmentRecipe recipe)
        {
            var sample = EquipmentRecipes.Create(recipe, 1); var inv = EquipmentInventory.Instance;
            if (inv == null) return 0;
            int n = 0;
            foreach (var e in inv.Items)
                if (e.slot == sample.slot && (e.slot == EquipmentSlot.Weapon ? e.weapon == sample.weapon : e.slot == EquipmentSlot.Armor ? e.armor == sample.armor : e.effect == sample.effect)) n++;
            return n;
        }
        private bool Wants(Order o) => o.mode == OrderMode.Forever || o.mode == OrderMode.Count ? o.amount > 0 : Stock(o.recipe) < o.amount;
        // 대기열이 비었을 때만 채운다: 목록 순서대로, 자원이 모자라면 다음 품목.
        public void Refill()
        {
            if (jobs.Count > 0 || Ruined || IsDead || !isActiveAndEnabled) return;
            foreach (var o in orders)
            {
                if (!Wants(o) || !TryEnqueue(o.recipe)) continue;
                if (o.mode == OrderMode.Count && --o.amount <= 0) orders.Remove(o);
                return;
            }
        }
        public CommanderAnt Crafter { get; private set; }
        public bool Ruined { get; private set; }
        public string WaitReason
        {
            get
            {
                if (Ruined || IsDead || !isActiveAndEnabled) return "도구: 제작대 사용 불가";
                if (jobs.Count == 0) return "재료 또는 주문 대기";
                if (EquipmentInventory.Instance == null || EquipmentInventory.Instance.Full) return "장비 보관함 없음/가득 참";
                if (Crafter != null) return "제작 중";
                var crew = AntUnitBase.Active.OfType<CommanderAnt>().Where(c => c.CivilianWorkReady && CanContinue(c)).ToArray();
                if (crew.Length == 0) return "담당 없음 · 원제작자 미복귀 시 보관/취소";
                var allowed = crew.Where(c => c.AllowsJob(CommanderJobs.Crafting)).ToArray();
                if (allowed.Length == 0) return "금지: 제작 작업";
                return allowed.Any(c => c.TryWorkApproach(Position, out _)) ? "담당 배정 대기" : "경로 없음";
            }
        }
        public bool CanContinue(CommanderAnt c) => c != null && jobs.Count > 0
            && (string.IsNullOrEmpty(jobs[0].makerId) || jobs[0].makerId == c.PersonalState.id);
        public bool CanAssign(CommanderAnt c) => !Ruined && isActiveAndEnabled && !IsDead && Crafter == null && jobs.Count > 0
            && CanContinue(c) && c.AllowsJob(CommanderJobs.Crafting)
            && c.CanChangeAllocation && !c.IsAwayFromHome && !c.IsDeployed && !c.IsWorking && !c.IsInCombat
            && (c.Agent == null || !c.Agent.hasPath && !c.Agent.pathPending)
            && Vector3.Distance(c.Position, Position) <= GameBalance.WorkshopRadius;
        public bool TryAssign(CommanderAnt c)
        {
            if (!CanAssign(c)) return false;
            c.CommandStop(); Crafter = c; c.CraftingWorkshop = this; c.SetWorkTarget(this); return true;
        }
        public void Release()
        {
            if (Crafter != null && Crafter.CraftingWorkshop == this) { Crafter.CraftingWorkshop = null; Crafter.SetWorkTarget(null); }
            Crafter = null;
        }
        public bool TryEnqueue(EquipmentRecipe recipe)
        {
            if (Ruined || IsDead || !isActiveAndEnabled || jobs.Count >= GameBalance.CraftQueueCapacity
                || !ScienceEffects.BuildingUnlocked(BuildingKind.Workshop) || !EquipmentRecipes.Unlocked(recipe)
                || EquipmentInventory.Instance == null || EquipmentInventory.Instance.Full || ResourceManager.Instance == null) return false;
            var cost = EquipmentRecipes.Cost(recipe);
            var rm = ResourceManager.Instance;
            if (rm.GetAmount(ResourceType.Food) < cost.x || rm.GetAmount(ResourceType.Soil) < cost.y || rm.GetAmount(ResourceType.Special) < cost.z) return false;
            jobs.Add(new Job { recipe = recipe }); return true;
        }
        public bool Cancel(int index)
        {
            if (index < 0 || index >= jobs.Count || ResourceManager.Instance == null) return false;
            var job = jobs[index]; var cost = EquipmentRecipes.Cost(job.recipe); var fraction = job.work > 0 ? .7f : 1f;
            if (job.materialsPaid)
            {
                ResourceManager.Instance.Add(ResourceType.Food, Mathf.FloorToInt(cost.x * fraction), ResourceReason.Refund);
                ResourceManager.Instance.Add(ResourceType.Soil, Mathf.FloorToInt(cost.y * fraction), ResourceReason.Refund);
                ResourceManager.Instance.Add(ResourceType.Special, cost.z, ResourceReason.Refund);
            }
            jobs.RemoveAt(index); if (index == 0) Release(); return true;
        }
        private void Update() { if (Time.deltaTime > 0) Refill(); Tick(Time.deltaTime); } // 정지·협상 중(timeScale 0)에는 생산 목록도 예약하지 않는다.
        public void Tick(float seconds)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds)) return;
            if (Ruined || IsDead || !isActiveAndEnabled || jobs.Count == 0) { Release(); return; }
            if (Crafter == null) return;
            if (!Crafter.CivilianWorkReady || !Crafter.AllowsJob(CommanderJobs.Crafting) || !Crafter.isActiveAndEnabled || Crafter.IsDead || Crafter.IsAwayFromHome || Crafter.IsEmbarked
                || Crafter.PersonalState.mentalBreak != MentalBreak.None || Vector3.Distance(Crafter.Position, Position) > GameBalance.WorkshopRadius)
            { Release(); return; }
            var inventory = EquipmentInventory.Instance;
            if (inventory == null || inventory.Full) return;
            var job = jobs[0];
            if (!string.IsNullOrEmpty(job.makerId) && job.makerId != Crafter.PersonalState.id) { Release(); return; }
            if (!job.materialsPaid)
            {
                var cost = EquipmentRecipes.Cost(job.recipe);
                if (ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(cost.x, cost.y, cost.z, reason: ResourceReason.Crafting)) { Release(); return; }
                job.materialsPaid = true;
            }
            job.makerId = Crafter.PersonalState.id;
            float speed = Crafter.WorkRate(CommanderActivity.Crafting);
            float elapsed = Mathf.Min(seconds, (GameBalance.CraftWork - job.work) / speed);
            job.work = Mathf.Min(GameBalance.CraftWork, job.work + elapsed * speed);
            Crafter.GainExperience(CommanderActivity.Crafting, elapsed, EquipmentRecipes.Topic(job.recipe));
            if (job.work < GameBalance.CraftWork) return;
            var item = EquipmentRecipes.Create(job.recipe, EquipmentRecipes.Quality(Crafter.Talents.Level(CommanderActivity.Crafting), UnityEngine.Random.value, UnityEngine.Random.value));
            if (!inventory.Add(item)) return;
            jobs.RemoveAt(0);
            AntColony.UI.ToastManager.Show("제작 완료: " + item.Label);
            Release();
        }
        protected override void OnDisable() { Release(); base.OnDisable(); }
        protected override void Die()
        {
            // 잔해에 대기열·진행도를 남겨 취소 환급과 저장이 가능하도록 한다.
            Ruined = true; enabled = false;
            var renderer = GetComponent<Renderer>(); if (renderer != null) renderer.material.color = Color.gray;
        }
        internal State CaptureState(List<CommanderAnt> commanders) => new State {
            materialVersion = 1,
            jobs = jobs.ConvertAll(j => new Job { recipe = j.recipe, work = j.work, makerId = j.makerId, materialsPaid = j.materialsPaid }), orders = orders.ConvertAll(o => new Order { recipe = o.recipe, mode = o.mode, amount = o.amount }), crafter = commanders.IndexOf(Crafter), ruined = Ruined, paused = !enabled, inactive = !gameObject.activeSelf };
        internal void RestoreState(State state, List<CommanderAnt> commanders)
        {
            Release(); jobs = state.jobs.ConvertAll(j => new Job { recipe = j.recipe, work = j.work, makerId = j.makerId, materialsPaid = state.materialVersion == 0 || j.materialsPaid });
            if (jobs.Count > 0 && jobs[0].work > 0 && string.IsNullOrEmpty(jobs[0].makerId) && state.crafter >= 0 && state.crafter < commanders.Count)
                jobs[0].makerId = commanders[state.crafter].PersonalState.id;
            orders = (state.orders ?? new List<Order>()).ConvertAll(o => new Order { recipe = o.recipe, mode = o.mode, amount = o.amount });
            Ruined = state.ruined; enabled = !state.paused && !Ruined;
            if (Ruined) { var renderer = GetComponent<Renderer>(); if (renderer != null) renderer.material.color = Color.gray; }
            if (!Ruined && !state.paused && !state.inactive && state.crafter >= 0 && state.crafter < commanders.Count) { Crafter = commanders[state.crafter]; Crafter.CraftingWorkshop = this; Crafter.SetWorkTarget(this); }
            gameObject.SetActive(!state.inactive);
        }
    }
}

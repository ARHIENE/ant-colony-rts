using System;
using System.Collections.Generic;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    public sealed class Workshop : BuildingBase
    {
        [Serializable] public class Job { public EquipmentRecipe recipe; public float work; }
        [Serializable] public class State
        {
            public List<Job> jobs = new List<Job>();
            public int crafter = -1;
            public bool ruined, paused, inactive;
        }
        private List<Job> jobs = new List<Job>();
        public IReadOnlyList<Job> Jobs => jobs;
        public CommanderAnt Crafter { get; private set; }
        public bool Ruined { get; private set; }
        public bool CanAssign(CommanderAnt c) => !Ruined && isActiveAndEnabled && !IsDead && Crafter == null && jobs.Count > 0
            && c != null && c.CanChangeAllocation && !c.IsAwayFromHome && !c.IsWorking && !c.IsInCombat
            && (c.Agent == null || !c.Agent.hasPath && !c.Agent.pathPending)
            && Vector3.Distance(c.Position, Position) <= GameBalance.WorkshopRadius;
        public bool TryAssign(CommanderAnt c)
        {
            if (!CanAssign(c)) return false;
            c.CommandStop(); Crafter = c; c.CraftingWorkshop = this; return true;
        }
        public void Release()
        {
            if (Crafter != null && Crafter.CraftingWorkshop == this) Crafter.CraftingWorkshop = null;
            Crafter = null;
        }
        public bool TryEnqueue(EquipmentRecipe recipe)
        {
            if (Ruined || IsDead || !isActiveAndEnabled || jobs.Count >= GameBalance.CraftQueueCapacity
                || !ScienceEffects.BuildingUnlocked(BuildingKind.Workshop) || !EquipmentRecipes.Unlocked(recipe)
                || EquipmentInventory.Instance == null || EquipmentInventory.Instance.Full || ResourceManager.Instance == null) return false;
            var cost = EquipmentRecipes.Cost(recipe);
            if (!ResourceManager.Instance.TrySpend(cost.x, cost.y, cost.z, reason: ResourceReason.Crafting)) return false;
            jobs.Add(new Job { recipe = recipe }); return true;
        }
        public bool Cancel(int index)
        {
            if (index < 0 || index >= jobs.Count || ResourceManager.Instance == null) return false;
            var job = jobs[index]; var cost = EquipmentRecipes.Cost(job.recipe); var fraction = job.work > 0 ? .5f : 1f;
            ResourceManager.Instance.Add(ResourceType.Food, Mathf.FloorToInt(cost.x * fraction), ResourceReason.Refund);
            ResourceManager.Instance.Add(ResourceType.Soil, Mathf.FloorToInt(cost.y * fraction), ResourceReason.Refund);
            ResourceManager.Instance.Add(ResourceType.Special, Mathf.FloorToInt(cost.z * fraction), ResourceReason.Refund);
            jobs.RemoveAt(index); if (jobs.Count == 0) Release(); return true;
        }
        private void Update() => Tick(Time.deltaTime);
        public void Tick(float seconds)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds) || Ruined || IsDead || !isActiveAndEnabled || jobs.Count == 0 || Crafter == null) return;
            if (!Crafter.isActiveAndEnabled || Crafter.IsDead || Crafter.IsAwayFromHome || Crafter.IsEmbarked
                || Crafter.PersonalState.mentalBreak != MentalBreak.None || Vector3.Distance(Crafter.Position, Position) > GameBalance.WorkshopRadius)
            { Release(); return; }
            var inventory = EquipmentInventory.Instance;
            if (inventory == null || inventory.Full) return;
            var job = jobs[0];
            float speed = Crafter.Talents.Multiplier(CommanderActivity.Crafting);
            float elapsed = Mathf.Min(seconds, (GameBalance.CraftWork - job.work) / speed);
            job.work = Mathf.Min(GameBalance.CraftWork, job.work + elapsed * speed);
            Crafter.GainExperience(CommanderActivity.Crafting, elapsed);
            if (job.work < GameBalance.CraftWork) return;
            var item = EquipmentRecipes.Create(job.recipe, EquipmentRecipes.Quality(Crafter.Talents.Level(CommanderActivity.Crafting), UnityEngine.Random.value, UnityEngine.Random.value));
            if (!inventory.Add(item)) return;
            jobs.RemoveAt(0);
            AntColony.UI.ToastManager.Show("제작 완료: " + item.Label);
            if (jobs.Count == 0) Release();
        }
        protected override void OnDisable() { Release(); base.OnDisable(); }
        protected override void Die()
        {
            // 잔해에 대기열·진행도를 남겨 취소 환급과 저장이 가능하도록 한다.
            Ruined = true; enabled = false;
            var renderer = GetComponent<Renderer>(); if (renderer != null) renderer.material.color = Color.gray;
        }
        internal State CaptureState(List<CommanderAnt> commanders) => new State {
            jobs = jobs.ConvertAll(j => new Job { recipe = j.recipe, work = j.work }), crafter = commanders.IndexOf(Crafter), ruined = Ruined, paused = !enabled, inactive = !gameObject.activeSelf };
        internal void RestoreState(State state, List<CommanderAnt> commanders)
        {
            Release(); jobs = state.jobs.ConvertAll(j => new Job { recipe = j.recipe, work = j.work });
            Ruined = state.ruined; enabled = !state.paused && !Ruined;
            if (Ruined) { var renderer = GetComponent<Renderer>(); if (renderer != null) renderer.material.color = Color.gray; }
            if (state.crafter >= 0) { Crafter = commanders[state.crafter]; Crafter.CraftingWorkshop = this; }
            gameObject.SetActive(!state.inactive);
        }
    }
}

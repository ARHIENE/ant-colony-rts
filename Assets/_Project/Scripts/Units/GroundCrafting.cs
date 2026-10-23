using System;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Units
{
    public enum GroundRecipe { StoneTools, ShortBow }
    [Serializable] public sealed class GroundCraftState
    {
        public int recipe = -1;
        public float work;
        public bool Valid => recipe >= -1 && recipe <= (int)GroundRecipe.ShortBow && !float.IsNaN(work) && !float.IsInfinity(work) && work >= 0 && work <= GroundCraftRecipes.Work;
    }

    public static class GroundCraftRecipes
    {
        // 작업량은 잠정. 비용·무연구 조건은 2026-10-23 G02/G34 확정.
        public const float Work = 10;
        public static string Name(GroundRecipe recipe) => recipe == GroundRecipe.StoneTools ? "돌 도구" : "단궁";
        public static (ResourceType type, int amount)[] Cost(GroundRecipe recipe) => recipe == GroundRecipe.StoneTools
            ? new[] { (ResourceType.Wood, 2), (ResourceType.Stone, 3) }
            : new[] { (ResourceType.Wood, 15), (ResourceType.Fiber, 5) };
        public static string CostLabel(GroundRecipe recipe) => string.Join(" · ", Cost(recipe).Select(c => c.type.DisplayName() + " " + c.amount));
        public static string Block(CommanderAnt c, GroundRecipe recipe)
        {
            if (!Enum.IsDefined(typeof(GroundRecipe), recipe)) return "알 수 없는 제작법";
            if (c == null || c.IsDead || !c.CanReceiveOrders || c.IsAwayFromHome || c.IsDeployed || c.IsCaptive) return "본거지에서 활동 중인 장수 필요";
            if (!c.AllowsJob(CommanderJobs.Crafting)) return "제작 작업 금지";
            if (c.PersonalState.groundCraft?.recipe >= 0) return "맨바닥 제작 진행 중";
            if (!c.CanChangeAllocation) return "현재 작업을 마친 뒤 제작 가능";
            if (recipe == GroundRecipe.StoneTools && c.PersonalState.toolTier >= 2) return "돌 도구 보유 중";
            if (recipe == GroundRecipe.ShortBow && EquipmentInventory.Instance?.Full != false) return "장비 보관함이 가득 참";
            var rm = ResourceManager.Instance;
            foreach (var cost in Cost(recipe))
                if (rm == null || rm.GetAmount(cost.type) < cost.amount) return $"재료 부족: {cost.type.DisplayName()} {rm?.GetAmount(cost.type) ?? 0}/{cost.amount}";
            return "";
        }
    }

    public partial class CommanderAnt
    {
        public bool IsGroundCrafting => PersonalState.groundCraft?.recipe >= 0;
        public bool StartGroundCraft(GroundRecipe recipe)
        {
            if (GroundCraftRecipes.Block(this, recipe) != "") return false;
            CommandStop(); ScienceAssignment?.ReleaseResearcher(); CraftingWorkshop?.Release();
            foreach (var cost in GroundCraftRecipes.Cost(recipe)) ResourceManager.Instance.TrySpend(cost.type, cost.amount, ResourceReason.Crafting);
            PersonalState.groundCraft = new GroundCraftState { recipe = (int)recipe };
            return true;
        }
        public bool TickGroundCraft(float seconds)
        {
            if (!IsGroundCrafting || IsDead || IsAwayFromHome || IsDeployed || !CanReceiveOrders || !CivilianWorkReady || IsInCombat || IsWorking || IsCarrying || IsConstructing
                || ScienceAssignment != null || CraftingWorkshop != null || !AllowsJob(CommanderJobs.Crafting)) return false;
            if (IsFlying ? !HasReachedDestination() : Agent != null && (Agent.pathPending || Agent.hasPath)) return false;
            if (!(seconds > 0) || float.IsInfinity(seconds)) return true;
            var state = PersonalState.groundCraft;
            var work = Mathf.Min(seconds * WorkRate(CommanderActivity.Crafting), GroundCraftRecipes.Work - state.work);
            state.work += work; GainExperience(CommanderActivity.Crafting, work);
            if (state.work < GroundCraftRecipes.Work) return true;
            if ((GroundRecipe)state.recipe == GroundRecipe.StoneTools) PersonalState.toolTier = Mathf.Max(2, PersonalState.toolTier);
            else
            {
                var item = new EquipmentItem { slot = EquipmentSlot.Weapon, weapon = WeaponKind.ShortBow, quality = 1 };
                if (EquipmentInventory.Instance == null || !EquipmentInventory.Instance.Add(item)) return true;
            }
            AntColony.UI.ToastManager.Show(CommanderName + ": " + GroundCraftRecipes.Name((GroundRecipe)state.recipe) + " 제작 완료");
            PersonalState.groundCraft = new GroundCraftState();
            return true;
        }
        public bool CancelGroundCraft()
        {
            if (!IsGroundCrafting || IsAwayFromHome || !CanReceiveOrders) return false;
            var state = PersonalState.groundCraft;
            foreach (var cost in GroundCraftRecipes.Cost((GroundRecipe)state.recipe))
                AntColony.World.DiplomacyManager.DropFloor(cost.type, Mathf.FloorToInt(cost.amount * (state.work > 0 ? .7f : 1f)), Position + ResourceManager.Scatter());
            PersonalState.groundCraft = new GroundCraftState(); return true;
        }
    }
}

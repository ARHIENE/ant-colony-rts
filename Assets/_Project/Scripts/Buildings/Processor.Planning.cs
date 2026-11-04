using System;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;

namespace AntColony.Buildings
{
    public sealed partial class Processor
    {
        public static int BatchesFor(int wanted, int stock, int batchSize) =>
            Math.Max(0, (int)Math.Ceiling(((double)wanted - stock) / batchSize));

        public string RecipePlan(Recipe recipe, Workshop.OrderMode mode, int quantity)
        {
            var batches = mode == Workshop.OrderMode.KeepStock ? BatchesFor(quantity, Kitchen.Stock(recipe.output), recipe.amount) : quantity;
            var rm = ResourceManager.Instance;
            var work = recipe.work * batches;
            var materials = string.Join(" / ", recipe.inputs.Select(i => $"{i.type.DisplayName()} {i.amount * batches}"));
            var missing = string.Join(" / ", recipe.inputs.Where(i => (rm?.GetAmount(i.type) ?? 0) + inputs.Amount(i.type) < i.amount)
                .Select(i => $"{i.type.DisplayName()} 부족 {i.amount - (rm?.GetAmount(i.type) ?? 0) - inputs.Amount(i.type)}"));
            var fluid = Chemical && recipe.output == ResourceType.Plastic ? $" · 원유 {IndustrialRecipes.PlasticOil * batches}"
                : Industrial && !Chemical ? $" · 가스 {batches}" : "";
            return $"1묶음 = {recipe.amount}개 · 작업량 {recipe.work:0} · 기준 속도1에서 분당 {60 * recipe.amount / recipe.work:0.0}개\n"
                + (mode == Workshop.OrderMode.Forever ? "계속 생산 · 아래는 지정 수량 기준 예산" : $"현재 목표 {batches}묶음 → {batches * recipe.amount}개")
                + $" · 기준 작업 {work:0}초\n{materials}{fluid}"
                + (missing.Length > 0 ? "\n첫 묶음: " + missing : "\n고체 원료 확보 · 실제 배송 필요")
                + (!MedicineResearch.Unlocked(recipe.output) ? "\n연구 필요: " + (recipe.output == ResourceType.MedicineAdvanced ? "정밀 약제" : "약제") : "")
                + "\n재고 유지는 바닥·완제품 운반 포함, 원료 배송 제외. 다른 제작대 진행분·이동·전력 대기·발효는 이 예상 시간에서 제외.";
        }

        public string RemainingWorkText
        {
            get
            {
                if (Current == null) return "";
                var left = Math.Max(0, Current.work - state.progress);
                var maker = AntUnitBase.Active.OfType<CommanderAnt>().FirstOrDefault(c => c.ServiceTarget == this && CanContinue(c)
                    && c.CivilianWorkReady && c.AllowsJob(Job) && (c.Position - Position).sqrMagnitude <= 49);
                var rate = maker != null && NeedsWork ? maker.WorkRate(Skill) : 0;
                return $"남은 작업량 {left:0.0} · " + (rate > 0 ? $"현재 속도 {rate:0.00} · 연속 작업 {left / rate:0}초" : "기여 0 · 담당/작업표/배송/공급 확인");
            }
        }
    }
}

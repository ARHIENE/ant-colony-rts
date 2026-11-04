using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.Core
{
    // 상태만 읽는다. 우선순위·예약·실제 작업 선택은 바꾸지 않는다.
    public static class WorkDiagnostics
    {
        private static readonly CommanderJobs[] Jobs = Enum.GetValues(typeof(CommanderJobs)).Cast<CommanderJobs>().Where(j => CommanderWorkState.Index(j) >= 0).ToArray();
        public static string Describe(Component target)
        {
            var protection = FixedTraitWork.Describe(target);
            return DescribeState(target) + (protection == null ? "" : "\n" + protection);
        }
        private static string DescribeState(Component target)
        {
            if (target == null) return "대상 없음";
            if (target is Kitchen k)
            {
                if (k.Paused) return "일시정지 — 재개를 누르세요.";
                if (k.Current == null && !k.IngredientNeeds.Any()) return k.Orders.Count == 0 ? "생산 목록 없음 — 요리를 추가하세요." : "목표 재고 충족 — 추가 생산은 목표를 높이세요.";
                var crew = Crew(k, CommanderJobs.Cooking); if (crew != null) return crew;
                if (k.Current == null && k.IngredientNeeds.Any(n => k.IngredientStock.Where(s => s.type == n.type).Sum(s => s.amount) < n.amount))
                    return Materials(k.IngredientNeeds, k.IngredientStock) + " — 요리 담당이 재료 창고에 갈 수 있는지 확인하세요.";
                return Working(k) + " · " + (k.Current?.name ?? "조리 준비");
            }
            if (target is Processor p)
            {
                if (p.Paused) return "일시정지 — 재개를 누르세요.";
                if (p.Brewing && (p.WaitReason?.Contains("발효") == true || p.WaitReason?.Contains("회수") == true)) return p.WaitReason;
                if (p.Current == null && p.Orders.Count == 0) return "생산 목록 없음 — 제조법을 추가하세요.";
                var crew = Crew(p, p.Job, p.CanContinue); if (crew != null) return crew;
                if (p.Current == null && p.IngredientNeeds.Any()) return Materials(p.IngredientNeeds, p.IngredientStock) + " — 작업표와 재료 창고까지의 길을 확인하세요.";
                return p.WaitReason + " — 전력·입출력과 원제작자 상태를 확인하세요.";
            }
            if (target is BuildingConstructionSite site)
            {
                if (site.Paused) return "일시정지 — 재개를 누르세요.";
                if (site.WaitingForUser) return "사용 중 — 이용자가 나온 뒤 공사를 시작합니다.";
                var crew = Crew(site, site.Ready ? site.Job : site.Job | CommanderJobs.Hauling); if (crew != null) return crew;
                return site.WaitReason switch
                {
                    BuildingConstructionSite.Wait.MaterialShortage => "건설 재료 부족 — 필요/예약/도착 수량을 확인하고 부족 품목을 공급하세요.",
                    BuildingConstructionSite.Wait.AwaitingHaul => "재료 배송 대기 — 건설 또는 운반 담당과 창고까지의 길을 확인하세요.",
                    _ => BuildingConstructionSite.Describe(site.WaitReason)
                };
            }
            if (target is ResourceNode node)
            {
                var reason = node.WorkWaitReason;
                if (reason == "담당 장수 대기" || reason == "채집 허용 장수 없음" || reason == "경로 차단")
                    return Crew(node, CommanderJobs.Gathering) ?? "담당 배정 대기 — 더 높은 작업·생활 요구가 끝나면 자동 수행합니다.";
                return reason + (reason.Contains("도구") ? " — 장수 장비와 자원 연구/표본을 확인하세요." : reason.Contains("금지") ? " — 대상의 채집 금지를 해제하세요." : "");
            }
            if (target is Workshop shop) return shop.WaitReason + " — 제작 작업표·원제작자·보관함을 확인하세요.";
            if (target is SupplyFacility supply) return supply.ContainerStatus + " — 제작/운반 담당과 용기·배관 연결을 확인하세요.";
            return "상태 확인";
        }
        private static string Materials(IEnumerable<(ResourceType type, int amount)> bill, IReadOnlyList<ProductionInputs.Stock> stock)
        {
            var missing = bill.Where(n => stock.Where(s => s.type == n.type).Sum(s => s.amount) < n.amount).ToArray();
            if (missing.Length == 0) return "재료 도착 완료 · 작업 배정 대기";
            return string.Join(" · ", missing.Select(n => $"{n.type.DisplayName()} 도착 {stock.Where(s => s.type == n.type).Sum(s => s.amount)}/{n.amount} · 창고 {ResourceManager.Instance?.GetAmount(n.type) ?? 0}"));
        }
        private static string Crew(Component target, CommanderJobs jobs, Func<CommanderAnt, bool> compatible = null)
        {
            var home = AntUnitBase.Active.OfType<CommanderAnt>().Where(c => c.IsColonyMember && !c.IsDead && c.isActiveAndEnabled).ToArray();
            if (home.Length == 0) return "담당 장수 없음 — 장수를 확보하세요.";
            var allowed = home.Where(c => Jobs.Any(j => (jobs & j) != 0 && c.AllowsJob(j))).ToArray();
            if (allowed.Length == 0) return "작업 금지 — 작업표에서 해당 작업을 켜세요.";
            var compatibleCrew = allowed.Where(c => compatible == null || compatible(c)).ToArray();
            if (compatibleCrew.Length == 0) return "원제작자 대기 — 원제작자 복귀 또는 미완성품 취소를 선택하세요.";
            var ready = compatibleCrew.Where(c => c.CivilianWorkReady).ToArray();
            if (ready.Length == 0) return "담당 부재 — 출전·원정·탑승·치료·수면 상태를 확인하세요.";
            if (!ready.Any(c => c.TryWorkApproach(target.transform.position, out _))) return "경로 차단 — 문·벽과 작업 지점까지의 길을 확인하세요.";
            return null;
        }
        private static string Working(Component target)
        {
            var worker = AntUnitBase.Active.OfType<CommanderAnt>().FirstOrDefault(c => c.ServiceTarget == target);
            if (worker != null) return (worker.Position - target.transform.position).sqrMagnitude > 49 ? "작업 지점으로 이동" : "작업 중";
            return "담당 배정 대기 — 작업표 우선순위와 식사·수면·치료를 확인하세요.";
        }
    }
}

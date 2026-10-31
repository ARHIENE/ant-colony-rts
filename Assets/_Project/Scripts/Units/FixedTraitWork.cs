using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    // 기존 귀속 저장을 그대로 사용한다. 직접 지시는 보호를 우회하여 담당을 교체할 수 있다.
    public static class FixedTraitWork
    {
        public static bool Allows(SoleWorker worker, string candidate, CommanderJobs job) =>
            job != CommanderJobs.Farming && job != CommanderJobs.Cooking && job != CommanderJobs.Art
            || worker?.Empowered != true || worker.id == candidate;
        private static SoleWorker For(Component target, out string effect)
        {
            effect = null;
            if (target == null) return null;
            if (target.TryGetComponent<FarmPlot>(out var farm)) { effect = "낙토"; return farm.Data.diamondCycle; }
            if (target is Kitchen kitchen) { effect = "식선"; return kitchen.FixedCook; }
            if (target is BuildingConstructionSite site && site.IsArt) { effect = "화룡"; return site.Artist; }
            return null;
        }
        public static bool Allows(Component target, CommanderAnt candidate, CommanderJobs job) => Allows(For(target, out _), candidate.PersonalState.id, job);
        public static string Describe(Component target)
        {
            var worker = For(target, out var effect);
            if (worker?.Empowered != true) return null;
            var owner = AntUnitBase.Active.OfType<CommanderAnt>().FirstOrDefault(c => c.PersonalState.id == worker.id);
            var job = effect == "낙토" ? CommanderJobs.Farming : effect == "식선" ? CommanderJobs.Cooking : CommanderJobs.Art;
            var reason = owner == null || owner.IsDead || !owner.IsColonyMember ? "담당 부재"
                : !owner.AllowsJob(job) ? "담당 작업 금지"
                : !owner.CivilianWorkReady || !owner.CanReceiveOrders ? "담당 생활/출정 상태 대기"
                : owner.LabUpgradeBusy || owner.IsWorking ? "담당 작업 중"
                : "담당 이동/자동 배정 대기";
            return $"{effect} 보호 · 고정 담당 {owner?.CommanderName ?? "없음"} · {reason}\n다른 장수 자동 배정 제외 · 직접 교체 시 효과 해제";
        }
    }
}

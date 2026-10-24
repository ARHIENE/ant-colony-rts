using System.Linq;
using AntColony.Boss;
using AntColony.Core;
using AntColony.World;
using UnityEngine;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        public string BossCarrier => "commander:" + PersonalState.id;
        public int BossCargoCount => CampaignResearch.Instance?.BossProgress.Cargo.Where(p => p.carrier == BossCarrier).Sum(p => p.count) ?? 0;

        public bool CommandCollectBossReward(BossStronghold target)
        {
            if (target == null || !target.Revealed || !CanReceiveOrders || LabUpgradeBusy || IsConstructing || IsWorking
                || IsCarrying || BossCargoCount > 0 || !CampaignResearch.Instance.BossProgress.Defeated(target.Id)
                || !CampaignResearch.Instance.CaptureState().bosses.unclaimed.Any(p => p.boss == (int)target.Id)
                || LoadCapacity < (target.Id == BossId.A ? 2 : 1)) return false;
            var site = target.GetComponent<ExpeditionSite>();
            var point = site?.Boss != null ? site.Boss.Position : target.transform.position;
            if (!TryWorkApproach(point, out var approach)) return false;
            CommandStop(); WorkState.bossPickup = (int)target.Id;
            SetMoveDestination(approach);
            TickBossRewardWork(); return true;
        }

        private bool TickBossRewardWork()
        {
            if (!CanReceiveOrders) return false;
            if (WorkState.bossPickup >= 0)
            {
                var target = FindObjectsByType<BossStronghold>(FindObjectsSortMode.None)
                    .FirstOrDefault(s => (int)s.Id == WorkState.bossPickup);
                if (target == null || !target.Revealed) { WorkState.bossPickup = -1; return false; }
                var site = target.GetComponent<ExpeditionSite>();
                var point = site?.Boss != null ? site.Boss.Position : target.transform.position;
                if ((Position - point).sqrMagnitude > 9 || !HasReachedDestination())
                {
                    if (!TryWorkApproach(point, out var approach)) { CommandStop(); return false; }
                    SetMoveDestination(approach); return true;
                }
                var id = target.Id;
                CommandStop();
                if (BossCargoCount == 0 && LoadCapacity >= (id == BossId.A ? 2 : 1)
                    && CampaignResearch.Instance.BossProgress.TryAcquire(id, BossCarrier, out _))
                { if (site != null) site.RewardsClaimed = true; }
            }
            if (BossCargoCount == 0 || IsAwayFromHome || IsEmbarked || IsHostile) return false;
            if ((Position - HomeReturnPoint.Anchor).sqrMagnitude > 49)
            {
                if (!IsWorking && !IsInCombat && TryWorkApproach(HomeReturnPoint.Anchor, out var approach))
                { SetMoveDestination(approach); return true; }
                return false;
            }
            foreach (var parcel in CampaignResearch.Instance.BossProgress.Cargo.Where(p => p.carrier == BossCarrier).ToArray())
                DeliverBossEquipment(parcel.id, Position);
            return false;
        }

        // 사망·영구 이탈·파괴에서만 호출. 씬 재구성과 정상 비활성화는 소실이 아니다.
        public void LoseBossCargo()
        {
            var progress = CampaignResearch.Instance?.BossProgress;
            if (progress == null) return;
            foreach (var parcel in progress.Cargo.Where(p => p.carrier == BossCarrier).ToArray()) progress.LoseCargo(parcel.id);
        }
    }
}

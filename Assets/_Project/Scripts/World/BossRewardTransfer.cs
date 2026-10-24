using System.Linq;
using AntColony.Boss;
using AntColony.Core;

namespace AntColony.World
{
    // ExpeditionTransport는 다른 담당자 소유다. 실제 하역·손실 처리도 이 API에 연결해야 한다.
    public static class BossRewardTransfer
    {
        public static string Carrier(ExpeditionTransport ship)
        {
            if (ship == null) return null;
            var identity = ship.GetComponent<BossCargoCarrier>() ?? ship.gameObject.AddComponent<BossCargoCarrier>();
            return identity.Id;
        }
        public static void RestoreCarrier(ExpeditionTransport ship, string savedId, int legacyIndex)
        {
            Carrier(ship);
            ship.GetComponent<BossCargoCarrier>().Id = string.IsNullOrWhiteSpace(savedId) ? "transport:" + legacyIndex : savedId;
        }
        public static int CargoLoad(ExpeditionTransport ship)
        {
            var progress = CampaignResearch.Instance?.BossProgress;
            var carrier = Carrier(ship);
            return progress == null || carrier == null ? 0 : progress.Cargo.Where(p => p.carrier == carrier).Sum(p => p.count);
        }
        public static bool TryCollect(ExpeditionTransport ship)
        {
            if (ship == null || ship.Site == null) return false;
            var stronghold = ship.Site.GetComponent<BossStronghold>();
            if (stronghold == null) return ship.TryCollectRewards();
            var progress = CampaignResearch.Instance?.BossProgress;
            if (progress == null || !stronghold.Revealed || ship.State != ExpeditionState.Deployed
                || ship.CargoCapacity - ship.CargoLoad < (stronghold.Id == AntColony.Boss.BossId.A ? 2 : 1)) return false;
            if (!ship.Crew.Any(c => c != null && c.CanReceiveOrders && (c.Position - ship.Position).sqrMagnitude <= 64)) return false;
            if (!progress.TryAcquire(stronghold.Id, Carrier(ship), out _)) return false;
            ship.Site.RewardsClaimed = true;
            return true;
        }
        // UnloadCargo의 시작에서 호출: 기본 화물 소비/설계도/장비보다 먼저 영구 반입 기록.
        public static void Deliver(ExpeditionTransport ship)
        {
            var research = CampaignResearch.Instance;
            var carrier = Carrier(ship);
            if (research == null || carrier == null || ship.State != ExpeditionState.Home) return;
            foreach (var p in research.BossProgress.Cargo.Where(p => p.carrier == carrier).ToArray()) AntColony.Units.CommanderAnt.DeliverBossEquipment(p.id, ship.Position);
        }
        public static void Lose(ExpeditionTransport ship)
        {
            var progress = CampaignResearch.Instance?.BossProgress;
            var carrier = Carrier(ship);
            if (progress == null || carrier == null) return;
            foreach (var p in progress.Cargo.Where(p => p.carrier == carrier).ToArray()) progress.LoseCargo(p.id);
        }
    }
}

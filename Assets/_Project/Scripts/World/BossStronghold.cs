using AntColony.Boss;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    // 거점 전체를 재생성하지 않는다. 바닥 전리품·시신·건물과 오래된 미회수 보상은 그대로 남긴다.
    public sealed class BossStronghold : MonoBehaviour
    {
        public BossId Id { get; private set; }
        private ExpeditionSite site;
        public bool Revealed => CampaignResearch.Instance?.BossProgress.Revealed(Id) == true;
        public void Initialize(BossId id, ExpeditionSite value)
        {
            Id = id; site = value;
            if (GetComponent<BossRewardVisual>() == null) gameObject.AddComponent<BossRewardVisual>();
            if (site.Boss != null)
            {
                site.Boss.CampaignId = id;
                (site.Boss.GetComponent<BossEncounter>() ?? site.Boss.gameObject.AddComponent<BossEncounter>()).Initialize(site.Boss, id);
            }
        }
        private void Update()
        {
            var progress = CampaignResearch.Instance?.BossProgress;
            if (progress == null || site == null || AntColony.Save.SaveSystem.Busy || !progress.Defeated(Id)) return;
            var allies = false; var combat = false; var transport = false;
            foreach (var unit in AntUnitBase.Active)
                if (unit != null && !unit.IsDead && !(unit is CommanderAnt captive && captive.IsCaptive)
                    && (unit.Position - transform.position).sqrMagnitude < 80 * 80)
                { allies = true; combat |= unit is SoldierAnt soldier && soldier.IsInCombat; }
            if (WorldMapManager.Instance != null)
                foreach (var ship in WorldMapManager.Instance.Transports)
                    if (ship != null && !ship.IsDead && ship.isActiveAndEnabled && (ship.Position - transform.position).sqrMagnitude < 80 * 80)
                        transport = true;
            if (site.Boss == null || !progress.TryRespawn(Id, GameCalendar.GameSeconds, allies, transport, combat)) return;
            site.RestoreState(false, site.Disposition);
            site.RewardsClaimed = false;
            site.Boss.Respawn();
        }
        public string Guide => "보스 " + Id + " · " + BossProgress.Hazard(Id) + "\n"
            + (Revealed ? "위치 공개됨" : CampaignResearch.Instance?.BossProgress.DetectionBlock(Id))
            + " / 보상: " + BossProgress.Reward(Id) + "\n" + BossEncounterRules.Guide(Id);
    }
}

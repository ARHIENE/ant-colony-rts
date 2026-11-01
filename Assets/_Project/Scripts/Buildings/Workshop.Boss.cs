using System.Linq;
using AntColony.Boss;
using AntColony.Core;
using AntColony.World;
using AntColony.Units;

namespace AntColony.Buildings
{
    public sealed partial class Workshop
    {
        public bool CanTakeOverBossJob(CommanderAnt next)
        {
            if (next == null || !next.CivilianWorkReady || !next.IsColonyMember || jobs.Count == 0
                || !jobs[0].bossJob || string.IsNullOrEmpty(jobs[0].makerId) || Crafter != null
                || Ruined || IsDead || Paused || !isActiveAndEnabled) return false;
            var owner = CommanderRoster.Instance?.Commanders.FirstOrDefault(c => c != null && c.PersonalState.id == jobs[0].makerId);
            return owner == null || owner.IsDead || owner.Social.departure == DepartureState.Left;
        }
        // 수동 인계는 작업물 소유자만 바꾼다. 결제·진행·전역 보스 상태를 다시 시작하지 않는다.
        public bool TryTakeOverBossJob(CommanderAnt next)
        {
            if (!CanTakeOverBossJob(next)) return false;
            var job = jobs[0]; var previous = job.makerId; job.makerId = next.PersonalState.id;
            CampaignHistory.Record("보스 제작 인계", next.CommanderName, job.Label + " · 이전 제작자 " + previous);
            return true;
        }
        public bool EnqueueBoss(BossId id, bool recovery, float remaining = BossProgress.RecoverySeconds, bool paid = false)
        {
            if (Ruined || IsDead || !isActiveAndEnabled || jobs.Count != 0 || Paused) return false;
            jobs.Add(new Job { bossJob = true, boss = (int)id, recovery = recovery, materialsPaid = paid,
                work = BossProgress.RecoverySeconds - remaining });
            return true;
        }
        public static bool HasBossJob => SceneObjects.All<Workshop>().Any(w => w.jobs.Any(j => j.bossJob));
        private bool CancelBossJob(int index)
        {
            var job = jobs[index];
            if (!job.materialsPaid) inputs.Drop(Position);
            else foreach (var cost in BossProgress.CraftCost((BossId)job.boss, job.recovery))
                DiplomacyManager.DropFloor(cost.Key, (int)(cost.Value * (job.work > 0 ? .7f : 1)), Position);
            CampaignResearch.Instance?.BossProgress.CancelCraft();
            jobs.RemoveAt(index); if (index == 0) Release(); return true;
        }
    }
}

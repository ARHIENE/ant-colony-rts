using System.Linq;
using AntColony.Boss;
using AntColony.Core;
using AntColony.World;

namespace AntColony.Buildings
{
    public sealed partial class Workshop
    {
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

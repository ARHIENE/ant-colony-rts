using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        private int CompetingRestPriority()
        {
            var fire = AntColony.Core.SceneObjects.All<WallFire>().Any(f => f.Burning && f.Managed);
            return AutoJobs.Concat(new[] { CommanderJobs.Research }).Where(AllowsJob)
                .Where(j => j != CommanderJobs.Firefighting || fire).Select(WorkState.Priority).DefaultIfEmpty(0).Max();
        }

        private bool NeedsCurrentTend(CommanderAnt p) => MedicalRules.NeedsDoctor(p.Health, MedicineAllowed(p.Health))
            || p.Health.medicine != MedicineLevel.NoCare && traits.Has(CommanderTrait.DiamondMedicine) && p.Health.wounds.Any(w => w.kind == WoundKind.Scar);

        private Infirmary ReachableRescueBed() => AntColony.Core.SceneObjects.All<Infirmary>()
            .Where(i => i.isActiveAndEnabled && !i.IsDead && i.HasFreeBed && TryWorkApproach(i.Position, out _))
            .OrderBy(i => (i.Position - Position).sqrMagnitude).FirstOrDefault();

        // 공동 작업과 같은 관계 예산: 실제 돌봄 1회는 공동 작업 60초의 +2를 사용한다.
        private void CareBond(CommanderAnt patient)
        {
            if (patient == this || !IsColonyMember || !patient.IsColonyMember) return;
            ChangeRelation(patient, 2);
        }

        private void RecordRelationMilestone(CommanderAnt other, float before, float after)
        {
            if (before < SocialRules.Friend && after >= SocialRules.Friend)
            {
                CampaignHistory.Record("관계 변화", CommanderName, other.CommanderName + "을 친구로 여깁니다", true);
                TellStory(other.CommanderName + "을 친구로 여김", other.CommanderName + ", 함께하니 든든해", 1);
            }
            else if (before > SocialRules.Rival && after <= SocialRules.Rival)
            {
                CampaignHistory.Record("관계 변화", CommanderName, other.CommanderName + "을 라이벌로 여깁니다", true);
                TellStory(other.CommanderName + "을 라이벌로 여김", other.CommanderName + "와는 맞지 않아", 1);
            }
        }

        public static string CareProgress(CommanderAnt patient)
        {
            var carers = Active.OfType<CommanderAnt>().Where(c => c.medPatient == patient && c.medJob != MedJob.None);
            return string.Join(" / ", carers.Select(c => c.medJob == MedJob.Rescue ? $"{c.CommanderName}: 구조 운반"
                : $"{c.CommanderName}: {(c.medJob == MedJob.Tend ? "처치" : c.medJob == MedJob.Feed ? "급식" : "수술")} · "
                    + (c.medJob == MedJob.Surgery ? "수술 목록에서 진행 확인" : $"작업 {c.medWork:0.#}/{(c.medJob == MedJob.Feed ? FeedSeconds : MedicalRules.TendSeconds * (c == patient ? 2 : 1)):0.#}")));
        }
    }
}

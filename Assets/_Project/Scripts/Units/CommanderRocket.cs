using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        // 로켓 파괴의 확정 생존·중상 규칙. 일반 피격의 즉사 추첨을 거치지 않는다.
        internal void SurviveRocketDestruction(Vector3 wreck)
        {
            SetEmbarked(false, wreck);
            CommandStop(); DropCargo(); LoseTroops(TroopCount, "로켓 파괴");
            ScienceAssignment?.ReleaseResearcher(); LabUpgradeLab?.CancelResearch(); CraftingWorkshop?.Release();
            WorkState.health = 0; WorkState.unconsciousSeconds = 0; WorkState.recoverySeconds = GameCalendar.SecondsPerDay;
            Health.wounds.Add(new Wound { part = BodyPart.LegL, kind = WoundKind.Bruise, damage = MedicalRules.MaxHp(BodyPart.LegL) * .75f });
            Health.wounds.Add(new Wound { part = BodyPart.LegR, kind = WoundKind.Bruise, damage = MedicalRules.MaxHp(BodyPart.LegR) * .75f });
            NotifyDowned(false, "로켓 파괴");
            CampaignHistory.Record("구조 필요", CommanderName, "로켓 잔해 주변 중상", true);
        }
    }
}

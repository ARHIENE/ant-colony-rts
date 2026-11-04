using System;
using System.Collections.Generic;
using AntColony.Units;

namespace AntColony.World
{
    // CommanderDuty의 자동 귀환 분기에서 Allows를 확인한다. 금지 파일 연결은 Claude 인계.
    [Serializable] public class HomeDefenseReturn
    {
        public List<string> disabled = new List<string>();
        public bool Enabled(CommanderAnt commander) => commander != null && !disabled.Contains(commander.PersonalState.id);
        public void Set(CommanderAnt commander, bool enabled)
        {
            if (commander == null) return;
            disabled.Remove(commander.PersonalState.id);
            if (!enabled) disabled.Add(commander.PersonalState.id);
        }
        public static bool Allows(CommanderAnt commander) => commander != null && commander.IsDeployed
            && !commander.IsReturning && !commander.IsAwayFromHome && !commander.IsCarrying && !commander.IsConstructing
            && !commander.IsWorking && commander.MedicalJob != MedJob.Rescue && commander.CorpseTarget == null
            && (WorldMapManager.Instance?.DefenseReturn.Enabled(commander) ?? true);
    }
}

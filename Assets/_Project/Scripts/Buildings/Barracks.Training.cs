using System.Collections.Generic;
using AntColony.Data;
using AntColony.Units;

namespace AntColony.Buildings
{
    // 병영 훈련(2026-10-31, 검토45 공백): 할 일이 없는 '사냥' 허용 장수가 병영 역할의 전투 기술을 작업의 절반 속도로 익힌다. 부상 없음.
    public partial class Barracks
    {
        public const float TrainingRate = .5f, TrainingSessionSeconds = 30f; // 잠정. 한 번 훈련 후 다시 작업을 고른다.
        private float trainingRemaining; // 저장하지 않는다: 불러오면 0이라 다음 판정에서 다시 작업을 고른다.
        public static IEnumerable<Barracks> Training => Active;
        public bool FreeForTraining => !IsDead && isActiveAndEnabled && !IsUpgrading;
        public CommanderActivity TrainedSkill => role == UnitRole.Ranged || role == UnitRole.Flying ? CommanderActivity.Ranged
            : role == UnitRole.Support ? CommanderActivity.Command : CommanderActivity.Melee;
        public bool BeginTraining() { trainingRemaining = TrainingSessionSeconds; return true; }
        public bool Train(CommanderAnt c, float seconds)
        {
            if (!FreeForTraining || !(seconds > 0) || float.IsInfinity(seconds) || trainingRemaining <= 0) return false;
            trainingRemaining -= seconds;
            c.GainExperience(TrainedSkill, seconds * TrainingRate);
            return true;
        }
    }
}

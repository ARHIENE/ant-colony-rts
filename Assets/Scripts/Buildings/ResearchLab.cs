using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    // 장수 개별 강화 연구소. 강화 수치는 장수가 소유하므로 연구소가 사라지거나 보직이 바뀌어도 유지된다.
    // 연구 시작은 선택한 장수의 현재 보직과 같은 역할의 연구소에서만 가능하다.
    public class ResearchLab : BuildingBase
    {
        [SerializeField] private UnitRole role = UnitRole.Melee;
        [SerializeField, Min(1)] private int maxLevel = 3;
        // 옛 역할 연구 비용(30/20)이 씬 템플릿에 직렬화돼 있어 필드 이름을 바꿔 새 기본값을 적용한다.
        [SerializeField, Min(0)] private int foodCostPerLevel = 45;
        [SerializeField, Min(0)] private int soilCostPerLevel = 30;
        [SerializeField, Min(0f)] private float researchTimeSeconds = 3f;

        private CommanderAnt target;
        // 대상 장수가 파괴되면 target은 Unity null이 되므로 진행 여부는 별도 플래그로 판단한다.
        private bool researching;
        // 코루틴 대신 남은 시간을 들고 있어야 저장/복원이 가능하다.
        private float remaining;
        private bool researchingAttack;

        internal float ResearchRemaining => researching ? remaining : 0f;
        internal bool ResearchIsAttack => researchingAttack;

        // 저장 복원 전용. 대상 장수가 없으면 아무것도 진행하지 않는다.
        internal void RestoreState(CommanderAnt commander, bool attack, float savedRemaining)
        {
            if (commander == null || savedRemaining <= 0f || commander.LabUpgradeBusy) return;
            target = commander;
            researching = true;
            researchingAttack = attack;
            remaining = savedRemaining;
            commander.LabUpgradeLab = this;
        }

        public UnitRole Role => role;
        public int MaxLevel => maxLevel;
        public bool IsResearching => researching;
        public CommanderAnt Target => target;

        protected override void OnDisable()
        {
            CancelResearch();
            base.OnDisable();
        }

        // 연구소 또는 대상 장수가 비활성화·파괴되면 즉시 중단한다. 완료되지 않고 비용은 환급되지 않는다(낚시 연구와 같은 규칙).
        public void CancelResearch()
        {
            remaining = 0f;
            Release();
        }

        private void Update() => Tick(Time.deltaTime);

        // 검사 스크립트가 시간을 직접 밀어 넣을 수 있도록 분리해 둔다.
        public void Tick(float seconds)
        {
            if (!isActiveAndEnabled || !researching || !(seconds > 0f) || float.IsInfinity(seconds)) return;
            // 대상 장수가 사라지면 완료하지 않고 그대로 중단한다(비용 환급 없음, 기존 규칙과 동일).
            if (target == null || !target.isActiveAndEnabled) { CancelResearch(); return; }
            remaining -= seconds;
            if (remaining > 0f) return;
            remaining = 0f;
            target.CompleteLabUpgrade(researchingAttack, maxLevel);
            AntColony.UI.ToastManager.Show(target.CommanderName + ": " + (researchingAttack ? "attack" : "armor") + " research complete.");
            Release();
        }

        public int GetFoodCost(int currentLevel) => foodCostPerLevel * (currentLevel + 1);
        public int GetSoilCost(int currentLevel) => soilCostPerLevel * (currentLevel + 1);

        public string GetAttackResearchLabel(CommanderAnt commander) => GetLabel(commander, true);
        public string GetArmorResearchLabel(CommanderAnt commander) => GetLabel(commander, false);

        private string GetLabel(CommanderAnt commander, bool attack)
        {
            var name = attack ? "ATK" : "Armor";
            if (commander == null) return $"{name}: Select 1 Commander";
            var level = attack ? commander.LabAttackLevel : commander.LabArmorLevel;
            if (commander.LabUpgradeBusy) return $"{name} Lv{level}\nUpgrading...";
            if (level >= maxLevel) return $"{name} Lv{level} (Max)";
            if (commander.Role != role) return $"{name} Lv{level}\nNo {commander.Role} Lab";
            if (IsResearching) return $"{name} Lv{level}\nLab Busy";
            return $"{name} Lv{level}>{level + 1}\n{GetFoodCost(level)}F {GetSoilCost(level)}S";
        }

        public bool TryResearchAttack(CommanderAnt commander) => TryStartResearch(commander, true);
        public bool TryResearchArmor(CommanderAnt commander) => TryStartResearch(commander, false);

        private bool TryStartResearch(CommanderAnt commander, bool attack)
        {
            if (!isActiveAndEnabled || IsResearching || commander == null || !commander.isActiveAndEnabled
                || commander.IsAwayFromHome
                || commander.LabUpgradeBusy || commander.Role != role || ResourceManager.Instance == null) return false;
            var level = attack ? commander.LabAttackLevel : commander.LabArmorLevel;
            if (level >= maxLevel) return false;
            if (!ResourceManager.Instance.TrySpend(GetFoodCost(level), GetSoilCost(level), reason: ResourceReason.Research)) return false;

            target = commander;
            researching = true;
            researchingAttack = attack;
            remaining = researchTimeSeconds;
            commander.LabUpgradeLab = this;
            return true;
        }

        private void Release()
        {
            if (target != null && target.LabUpgradeLab == this) target.LabUpgradeLab = null;
            target = null;
            researching = false;
        }
    }
}

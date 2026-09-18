using System.Collections;
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
            StopAllCoroutines();
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
                || commander.Transport != null
                || commander.LabUpgradeBusy || commander.Role != role || ResourceManager.Instance == null) return false;
            var level = attack ? commander.LabAttackLevel : commander.LabArmorLevel;
            if (level >= maxLevel) return false;
            if (!ResourceManager.Instance.TrySpend(GetFoodCost(level), GetSoilCost(level))) return false;

            target = commander;
            researching = true;
            commander.LabUpgradeLab = this;
            StartCoroutine(ResearchRoutine(attack));
            return true;
        }

        private IEnumerator ResearchRoutine(bool attack)
        {
            yield return new WaitForSeconds(researchTimeSeconds);
            // 장수 쪽 비활성화가 이 코루틴을 멈추므로 여기까지 오면 대상은 유효하다.
            target.CompleteLabUpgrade(attack, maxLevel);
            Release();
        }

        private void Release()
        {
            if (target != null && target.LabUpgradeLab == this) target.LabUpgradeLab = null;
            target = null;
            researching = false;
        }
    }
}

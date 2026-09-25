using AntColony.Buildings;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    // 적 세력의 장수. 침공 파동을 이끌고, 쓰러지면 포로가 될 수 있다.
    //
    // 설계 메모(기획 미정 사항을 프로토타입에서 이렇게 정함):
    // 기획의 "개미가 모두 소모되면 장수가 노출되어 먹히거나, 포로가 되거나, 죽음" 중
    // 포로 경로만 구현한다. 플레이어 장수의 사망 규칙(CommanderAnt.IsDead == false)은 건드리지 않았다.
    // 적 장수는 일반개미 풀을 쓰지 않으므로 WildMonster의 체력을 그대로 쓰고,
    // 체력이 0이 되는 시점을 "무력화"로 보아 수용소가 있으면 포로로 넘긴다.
    public class EnemyCommander : WildMonster
    {
        // ponytail: 포로 전환 확률과 기본 성격 범위는 1차 프로토타입 잠정값이다.
        [SerializeField] private string commanderName = "Enemy Commander";
        [SerializeField] private CommanderRank rank = CommanderRank.Sergeant;
        [SerializeField] private UnitRole[] roles = { UnitRole.Worker, UnitRole.Melee };
        [SerializeField] private CommanderTraits traits = new CommanderTraits();
        [SerializeField] private CommanderTalents talents = new CommanderTalents();
        [SerializeField, Range(0f, 1f)] private float captureChance = 1f;

        public string CommanderName => commanderName;
        public CommanderRank Rank => rank;
        public UnitRole[] Roles => roles;
        public CommanderTraits Traits => traits;
        public CommanderTalents Talents => talents;
        internal void RestoreTalents(CommanderTalents value) => talents = value.Copy();
        public bool WasCaptured { get; private set; }

        // 적 소굴이 파동마다 장수를 만들 때 개성을 심어준다.
        public void ConfigureCommander(string displayName, CommanderRank commanderRank, UnitRole[] allowedRoles, CommanderTraits value)
        {
            if (!string.IsNullOrEmpty(displayName)) commanderName = displayName;
            rank = commanderRank;
            if (allowedRoles != null && allowedRoles.Length > 0) roles = allowedRoles;
            if (value != null) traits = value;
            talents.Generate(traits);
        }

        // 수용소가 있고 정원이 남아 있으면 포로가 된다. 아니면 평소대로 죽는다.
        protected override void Die()
        {
            var camp = PrisonerCamp.Instance;
            if (camp != null && camp.HasSpace && Random.value <= captureChance
                && camp.TryCapture(commanderName, rank, roles, traits, talents))
            {
                WasCaptured = true;
                camp.Prisoners[camp.Count - 1].PersonalState.originFaction = GetComponentInParent<ExpeditionSite>()?.Faction ?? "Local enemy";
            }
            base.Die();
        }
    }
}

using System.Collections.Generic;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Core
{
    // 플레이어 장수의 생성 창구이자 명부. 시작 장수뿐 아니라 번식·영입·포로 회유로 합류하는 장수도
    // 전부 이 컴포넌트를 거치므로, 장수를 만드는 방법은 여기 한 곳에만 있다.
    public class CommanderRoster : MonoBehaviour
    {
        public static CommanderRoster Instance { get; private set; }

        [SerializeField] private UnitData[] roleProfiles;
        [SerializeField] private Transform spawnOrigin;
        // ponytail: 초기 장수/병력 수는 임시 시작 설정이다.
        [SerializeField, Min(1)] private int startingCommanders = 12;
        [SerializeField, Min(0)] private int troopsPerCommander = 2;
        [SerializeField, Min(1f)] private float spawnSampleRadius = 10f;

        private readonly List<CommanderAnt> commanders = new List<CommanderAnt>();
        private int nextCommanderNumber = 1;

        public IReadOnlyList<CommanderAnt> Commanders
        {
            get
            {
                commanders.RemoveAll(commander => commander == null);
                return commanders;
            }
        }

        // 세력 규모. 스카우트 영입 성공률이 이 값을 읽는다.
        public int Count => Commanders.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (roleProfiles == null || roleProfiles.Length == 0 || roleProfiles[0] == null || AntPool.Instance == null) return;
            var origin = spawnOrigin != null ? spawnOrigin.position : transform.position;
            for (var i = 0; i < startingCommanders; i++)
            {
                var position = origin + new Vector3((i % 4 - 1.5f) * 2f, 0f, 4f + i / 4 * 2f);
                var combatRole = (UnitRole)(1 + i % 5);
                var commander = Create(null, CommanderRank.Sergeant, new[] { UnitRole.Worker, combatRole },
                    UnitRole.Worker, CommanderTraits.Random(), position);
                commander?.TryAssign(troopsPerCommander);
            }
        }

        // 장수 한 명을 만들어 명부에 넣는다. displayName이 비면 일련번호로 자동 명명한다.
        // NavMesh 위에 자리를 잡지 못하면 아무것도 만들지 않고 null을 돌려준다(반쯤 생성된 장수를 남기지 않는다).
        public CommanderAnt Create(string displayName, CommanderRank rank, IEnumerable<UnitRole> roles,
            UnitRole startRole, CommanderTraits traits, Vector3 position)
        {
            if (roleProfiles == null || roleProfiles.Length == 0 || roleProfiles[0] == null) return null;
            if (!NavMesh.SamplePosition(position, out var hit, spawnSampleRadius, NavMesh.AllAreas)) return null;

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = string.IsNullOrEmpty(displayName) ? $"Commander {nextCommanderNumber}" : displayName;
            go.transform.SetParent(transform);
            go.transform.position = hit.position;
            go.transform.localScale = Vector3.one * .6f;
            nextCommanderNumber++;

            var commander = go.AddComponent<CommanderAnt>();
            commander.ConfigureCommander(go.name, rank, roles, startRole);
            commander.Initialize(roleProfiles[0], null, null);
            commander.SetRoleProfiles(roleProfiles);
            if (traits != null) commander.ApplyTraits(traits);
            commanders.Add(commander);
            return commander;
        }

        public void SetRoleProfiles(UnitData[] profiles) => roleProfiles = profiles;
    }
}

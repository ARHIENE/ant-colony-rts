using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Core
{
    public class CommanderRoster : MonoBehaviour
    {
        [SerializeField] private UnitData[] roleProfiles;
        [SerializeField] private Transform spawnOrigin;
        // ponytail: 초기 장수/병력 수는 임시 시작 설정이다. 번식·영입은 별도 기획 단계다.
        [SerializeField, Min(1)] private int startingCommanders = 12;
        [SerializeField, Min(0)] private int troopsPerCommander = 2;

        private void Start()
        {
            if (roleProfiles == null || roleProfiles.Length == 0 || roleProfiles[0] == null || AntPool.Instance == null) return;
            var origin = spawnOrigin != null ? spawnOrigin.position : transform.position;
            for (var i = 0; i < startingCommanders; i++)
            {
                var position = origin + new Vector3((i % 4 - 1.5f) * 2f, 0f, 4f + i / 4 * 2f);
                if (!NavMesh.SamplePosition(position, out var hit, 10f, NavMesh.AllAreas)) continue;
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = $"Commander {i + 1}";
                go.transform.SetParent(transform);
                go.transform.position = hit.position;
                go.transform.localScale = Vector3.one * .6f;
                var commander = go.AddComponent<CommanderAnt>();
                var combatRole = (UnitRole)(1 + i % 5);
                commander.ConfigureCommander(go.name, CommanderRank.Sergeant, new[] { UnitRole.Worker, combatRole }, UnitRole.Worker);
                commander.Initialize(roleProfiles[0], null, null);
                commander.SetRoleProfiles(roleProfiles);
                commander.TryAssign(troopsPerCommander);
            }
        }
    }
}

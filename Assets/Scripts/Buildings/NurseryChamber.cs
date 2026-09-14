using System.Collections.Generic;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    // 번식(연애·짝짓기). 기획: 장수끼리 호감도가 쌓여 조건을 충족하면 출산하고,
    // 태어난 장수는 부모 유전 + 랜덤 스탯을 가진다. 장수 x 장수만 가능하며 이 건물 없이는 출산할 수 없다.
    // 이 컴포넌트가 곧 양육실이므로, 건물이 파괴되거나 비활성화되면 Update가 멈춰 번식도 멈춘다.
    public class NurseryChamber : MonoBehaviour
    {
        // ponytail: 호감도 수치·비용·한도는 전부 1차 프로토타입 잠정값이다. 기획 확정 시 이 필드만 고치면 된다.
        [SerializeField, Min(0.1f)] private float affinityRadius = 5f;
        [SerializeField, Min(0.1f)] private float affinityPerSecond = 10f;
        [SerializeField, Min(1f)] private float birthAffinity = 100f;
        [SerializeField, Min(0)] private int birthFoodCost = 30;
        [SerializeField, Min(1)] private int maxCommanders = 20;
        [SerializeField, Min(0)] private int newbornTroops;

        // 순서에 무관한 장수 쌍. XOR 해시와 양방향 비교라 (A,B)와 (B,A)가 같은 항목이 된다.
        private readonly struct Pair : System.IEquatable<Pair>
        {
            private readonly CommanderAnt first;
            private readonly CommanderAnt second;

            public Pair(CommanderAnt first, CommanderAnt second)
            {
                this.first = first;
                this.second = second;
            }

            // 파괴된 장수가 낀 쌍은 더 이상 유효하지 않다.
            public bool IsAlive => first != null && second != null;

            public bool Equals(Pair other)
                => first == other.first && second == other.second || first == other.second && second == other.first;

            public override bool Equals(object obj) => obj is Pair other && Equals(other);

            public override int GetHashCode()
                => (first != null ? first.GetHashCode() : 0) ^ (second != null ? second.GetHashCode() : 0);
        }

        // 쌍별 누적 호감도. 12명 규모에서 66쌍이라 사전 하나로 충분하다.
        // ponytail: O(n^2) 쌍 스캔이다. 장수가 수백 명이 되면 공간 분할로 바꾼다.
        private readonly Dictionary<Pair, float> affinity = new Dictionary<Pair, float>();
        private readonly List<Pair> expired = new List<Pair>();

        public int BirthCount { get; private set; }

        private void Update() => Tick(Time.deltaTime);

        // 검사 스크립트가 시간을 직접 밀어 넣을 수 있도록 분리해 둔다.
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            var roster = CommanderRoster.Instance;
            if (roster == null) return;

            var commanders = roster.Commanders;
            var radiusSquared = affinityRadius * affinityRadius;

            for (var i = 0; i < commanders.Count; i++)
                for (var j = i + 1; j < commanders.Count; j++)
                {
                    var first = commanders[i];
                    var second = commanders[j];
                    if (first == null || second == null) continue;
                    if (!first.isActiveAndEnabled || !second.isActiveAndEnabled) continue;
                    if ((first.Position - second.Position).sqrMagnitude > radiusSquared) continue;

                    var key = new Pair(first, second);
                    var value = (affinity.TryGetValue(key, out var current) ? current : 0f) + affinityPerSecond * deltaTime;
                    if (value >= birthAffinity && TryGiveBirth(first, second))
                    {
                        affinity.Remove(key);
                        continue;
                    }
                    affinity[key] = value;
                }

            PruneMissing();
        }

        // 출산 조건: 정원 여유 + 식량 지불 + NavMesh 위 자리. 하나라도 실패하면 호감도를 유지한 채 다음 기회를 노린다.
        private bool TryGiveBirth(CommanderAnt first, CommanderAnt second)
        {
            var roster = CommanderRoster.Instance;
            if (roster == null || roster.Count >= maxCommanders) return false;
            if (birthFoodCost > 0 && ResourceManager.Instance != null
                && !ResourceManager.Instance.TrySpend(birthFoodCost, 0)) return false;

            var roles = InheritRoles(first, second);
            var child = roster.Create(null, CommanderRank.Corporal, roles, roles[0],
                CommanderTraits.Inherit(first.Traits, second.Traits), transform.position);

            if (child == null)
            {
                // 자리를 못 잡아 생성이 취소됐으면 이미 낸 식량을 돌려준다.
                if (birthFoodCost > 0) ResourceManager.Instance?.Add(ResourceType.Food, birthFoodCost);
                return false;
            }

            if (newbornTroops > 0) child.TryAssign(newbornTroops);
            BirthCount++;
            return true;
        }

        // 기획: 보직 일부를 계승한다. 일개미는 공통이고 부모의 전투 보직 중 하나를 물려받는다.
        private static UnitRole[] InheritRoles(CommanderAnt first, CommanderAnt second)
        {
            var parent = Random.value < .5f ? first : second;
            foreach (var role in parent.AllowedRoles)
                if (role != UnitRole.Worker) return new[] { UnitRole.Worker, role };
            return new[] { UnitRole.Worker };
        }

        // 파괴된 장수가 낀 쌍을 정리해 사전이 무한히 커지지 않게 한다.
        private void PruneMissing()
        {
            if (affinity.Count == 0) return;
            expired.Clear();
            foreach (var pair in affinity)
                if (!pair.Key.IsAlive) expired.Add(pair.Key);
            foreach (var key in expired) affinity.Remove(key);
        }

        public float GetAffinity(CommanderAnt first, CommanderAnt second)
            => affinity.TryGetValue(new Pair(first, second), out var value) ? value : 0f;
    }
}

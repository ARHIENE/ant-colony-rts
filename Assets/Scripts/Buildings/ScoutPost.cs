using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    // 스카우트 영입. 기획: 스카우터개미를 맵에 보내면 야생/적 소굴의 장수를 발견해 영입을 제안하며,
    // 세력 규모가 클수록 성공률이 올라가는 확률형이다. 파견은 한 번에 한 건만 진행된다.
    public class ScoutPost : MonoBehaviour
    {
        // ponytail: 비용·확률·소요시간은 1차 프로토타입 잠정값이다. 난이도 설정이 생기면 그쪽에서 가져온다.
        [SerializeField, Min(0)] private int dispatchFoodCost = 20;
        [SerializeField, Min(0)] private int dispatchAnts = 1;
        [SerializeField, Min(0.1f)] private float travelSeconds = 30f;
        [SerializeField, Range(0f, 1f)] private float baseChance = .25f;
        [SerializeField, Range(0f, 1f)] private float chancePerCommander = .02f;
        [SerializeField, Range(0f, 1f)] private float maxChance = .8f;
        [SerializeField, Min(1)] private int maxCommanders = 20;

        private float remaining;
        // 실제로 차출한 개미 수. 설정값이 아니라 이 값을 돌려줘야 중복 반납·유실이 없다.
        private int dispatchedAnts;

        public int DispatchFoodCost => dispatchFoodCost;
        public int DispatchAnts => dispatchAnts;
        public bool IsDispatched { get; private set; }
        public float Remaining => remaining;
        public int SuccessCount { get; private set; }
        public int FailureCount { get; private set; }

        // 세력이 클수록 영입 제안을 받아들일 확률이 높다. 상한을 둬 확정 영입이 되지 않게 한다.
        public float CurrentChance
        {
            get
            {
                var size = CommanderRoster.Instance != null ? CommanderRoster.Instance.Count : 0;
                return Mathf.Clamp(baseChance + size * chancePerCommander, 0f, maxChance);
            }
        }

        private void Update() => Tick(Time.deltaTime);

        // 파견 중에 건물이 부서지거나 꺼지면 파견을 취소하고 나가 있던 개미를 돌려준다.
        // 그냥 두면 개미가 Assigned에 갇혀 영영 돌아오지 않는다.
        private void OnDisable()
        {
            IsDispatched = false;
            remaining = 0f;
            ReturnScouts();
        }

        // 반납은 여기 한 곳만 거친다. 이미 돌려준 뒤에는 0이라 두 번 반납되지 않는다.
        private void ReturnScouts()
        {
            if (dispatchedAnts <= 0) return;
            var count = dispatchedAnts;
            dispatchedAnts = 0;
            AntPool.Instance?.ReturnAssigned(count);
        }

        public string GetStatusLabel()
        {
            var status = IsDispatched
                ? $"Scouting... {remaining:0.0}s left"
                : $"Idle  Cost {dispatchFoodCost}F {dispatchAnts} Ant";
            return $"Scout Post  {status}\nJoin chance {CurrentChance:P0}   Recruited {SuccessCount}  Failed {FailureCount}";
        }

        // 검사 스크립트가 시간을 직접 밀어 넣을 수 있도록 분리해 둔다.
        public void Tick(float deltaTime)
        {
            if (!IsDispatched || deltaTime <= 0f) return;
            remaining -= deltaTime;
            if (remaining > 0f) return;

            IsDispatched = false;
            remaining = 0f;
            Resolve();
        }

        // 스카우터를 내보낸다. 이미 나가 있거나 비용을 못 내면 거부한다.
        // 파견 개미는 돌아올 때까지 대기 풀에서 빠진다(건설 차출과 같은 취급).
        public bool TryDispatch()
        {
            if (IsDispatched || !isActiveAndEnabled) return false;
            if (dispatchFoodCost > 0 && ResourceManager.Instance != null
                && !ResourceManager.Instance.CanAfford(dispatchFoodCost, 0)) return false;
            if (dispatchAnts > 0)
            {
                if (AntPool.Instance == null || !AntPool.Instance.TryAssign(dispatchAnts)) return false;
                dispatchedAnts = dispatchAnts;
            }

            if (dispatchFoodCost > 0 && ResourceManager.Instance != null
                && !ResourceManager.Instance.TrySpend(dispatchFoodCost, 0))
            {
                // 지불에 실패하면 차출한 개미를 즉시 되돌린다.
                ReturnScouts();
                return false;
            }

            IsDispatched = true;
            remaining = travelSeconds;
            return true;
        }

        // 귀환 판정. 성공하면 새 장수가 합류하고, 실패해도 파견 개미는 돌아온다.
        private void Resolve()
        {
            ReturnScouts();

            var roster = CommanderRoster.Instance;
            if (roster == null || roster.Count >= maxCommanders || Random.value > CurrentChance)
            {
                FailureCount++;
                return;
            }

            var traits = CommanderTraits.Random();
            var combatRole = (UnitRole)Random.Range(1, 6);
            var recruit = roster.Create(null, CommanderRank.Corporal,
                new[] { UnitRole.Worker, combatRole }, UnitRole.Worker, traits, transform.position);

            if (recruit == null) FailureCount++;
            else SuccessCount++;
        }
    }
}

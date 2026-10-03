using AntColony.Core;
using AntColony.Boss.AoE;
using UnityEngine;
using UnityEngine.Events;

namespace AntColony.Boss
{
    // SIMUL-TeaamProject(hyeonyeop 브랜치) BossHealth.cs 참고 포팅.
    // 자체 IDamageable 대신 프로젝트 공용 AntColony.Core.IDamageable을 구현해 SoldierAnt.CommandAttack 등과 그대로 호환됨.
    public class BossHealth : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        [SerializeField] private float maxHp = 1000f;

        // ponytail: 잠정 전리품 수치. 기존 씬 보스는 필드가 없으므로 이 기본값을 그대로 받는다.
        [Header("Loot")]
        [SerializeField, Min(0)] private int foodReward = 100;
        [SerializeField, Min(0)] private int specialReward = 20;

        [Header("Events")]
        public UnityEvent<float, float> onHPChanged;
        public UnityEvent onDead;

        private float currentHp;
        private bool deathProcessed;

        public float CurrentHp => currentHp;
        public float MaxHp => maxHp;
        public bool IsDead => currentHp <= 0f;
        public Vector3 Position => transform.position;
        public float RootRemaining { get; private set; }
        private bool resumeMovement;
        public void Root(float seconds)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds) || IsDead) return;
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                if (RootRemaining <= 0) resumeMovement = !agent.isStopped;
                agent.isStopped = true;
            }
            RootRemaining = Mathf.Max(RootRemaining, seconds);
        }
        private void LateUpdate()
        {
            if (RootRemaining <= 0) return;
            RootRemaining = Mathf.Max(0, RootRemaining - Time.deltaTime);
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = RootRemaining > 0 || !resumeMovement;
        }

        private void Awake()
        {
            currentHp = maxHp;
        }

        // 원정 복제본을 활성화하기 전에 한 번 적용하는 임시 밸런스.
        internal void ConfigureExpedition(int difficulty)
        {
            var multiplier = Mathf.Max(1, difficulty);
            maxHp *= multiplier;
            currentHp = maxHp;
            foodReward *= multiplier;
            specialReward *= multiplier;
        }

        // 저장 복원 전용. 죽은 상태로는 복원하지 않는다(사망 처리는 전리품/이벤트를 동반하므로 되돌릴 수 없다).
        internal void RestoreHp(float value)
        {
            currentHp = Mathf.Clamp(value, 0f, maxHp);
            onHPChanged?.Invoke(currentHp, maxHp);
        }

        internal bool DeathProcessed => deathProcessed;

        // 저장 복원 전용. 이미 쓰러져 있던 보스를 전리품/이벤트 없이 쓰러진 상태로만 되돌린다.
        internal void RestoreDefeated()
        {
            currentHp = 0f;
            deathProcessed = true;
            onHPChanged?.Invoke(0f, maxHp);
            gameObject.SetActive(false);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            currentHp = Mathf.Max(0f, currentHp - amount);
            onHPChanged?.Invoke(currentHp, maxHp);

            if (currentHp <= 0f)
                Die();
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            currentHp = Mathf.Min(maxHp, currentHp + amount);
            onHPChanged?.Invoke(currentHp, maxHp);
        }

        private void Die()
        {
            // onHPChanged 구독자가 비치명 피격 중 치명타를 넣으면 안쪽/바깥쪽 TakeDamage가 모두 여기로 온다.
            if (deathProcessed) return;
            deathProcessed = true;

            var circle = GetComponent<BossCircleAoE>();
            if (circle != null) circle.enabled = false;
            var cone = GetComponent<BossConeAoE>();
            if (cone != null) cone.enabled = false;
            var line = GetComponent<BossLineAoE>();
            if (line != null) line.enabled = false;

            var loop = GetComponent<BossBasicPatternLoop>();
            if (loop != null) loop.enabled = false;

            var sequence = GetComponent<BossPatternSequenceSimple>();
            if (sequence != null) sequence.enabled = false;

            // 이벤트 구독자가 보스를 파괴해도 전리품이 남도록 이벤트보다 먼저 떨군다.
            var radius = 0f;
            foreach (var body in GetComponentsInChildren<Collider>())
            {
                var offset = body.bounds.center - transform.position;
                radius = Mathf.Max(radius, new Vector2(offset.x, offset.z).magnitude
                    + Mathf.Max(body.bounds.extents.x, body.bounds.extents.z));
            }
            BossLoot.Drop(transform.position, radius, foodReward, specialReward);
            CampaignHistory.Record("보스 처치", gameObject.name, GetComponentInParent<AntColony.World.ExpeditionSite>()?.Title ?? "본거지");

            GameManager.Instance?.ReportBossDefeated();
            onDead?.Invoke();
        }
    }
}

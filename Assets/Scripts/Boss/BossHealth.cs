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

        private void Awake()
        {
            currentHp = maxHp;
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

            GameManager.Instance?.ReportBossDefeated();
            onDead?.Invoke();
        }
    }
}

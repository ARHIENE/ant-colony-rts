using AntColony.Core;
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

        [Header("Events")]
        public UnityEvent<float, float> onHPChanged;
        public UnityEvent onDead;

        private float currentHp;

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
            var loop = GetComponent<BossBasicPatternLoop>();
            if (loop != null) loop.enabled = false;

            var sequence = GetComponent<BossPatternSequenceSimple>();
            if (sequence != null) sequence.enabled = false;

            GameManager.Instance?.ReportBossDefeated();
            onDead?.Invoke();
        }
    }
}

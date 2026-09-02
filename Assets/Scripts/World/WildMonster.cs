using System.Collections.Generic;
using AntColony.Core;
using UnityEngine;

namespace AntColony.World
{
    public class WildMonster : MonoBehaviour, IDamageable
    {
        private static readonly List<WildMonster> Active = new List<WildMonster>();

        [SerializeField] private float maxHealth = 150f;
        private float currentHealth;

        public bool IsDead => currentHealth <= 0f;
        public Vector3 Position => transform.position;

        private void OnEnable()
        {
            currentHealth = maxHealth;
            Active.Add(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            currentHealth -= amount;
            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                gameObject.SetActive(false);
                GameManager.Instance?.ReportWildMonsterDefeated();
            }
        }

        public static WildMonster FindNearest(Vector3 from, float maxRadius)
        {
            WildMonster nearest = null;
            var nearestDistSqr = maxRadius * maxRadius;
            foreach (var monster in Active)
            {
                if (monster == null || monster.IsDead) continue;
                var distSqr = (monster.transform.position - from).sqrMagnitude;
                if (distSqr <= nearestDistSqr)
                {
                    nearestDistSqr = distSqr;
                    nearest = monster;
                }
            }
            return nearest;
        }
    }
}

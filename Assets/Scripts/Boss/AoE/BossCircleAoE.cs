using System.Collections;
using System.Collections.Generic;
using AntColony.Boss.Telegraph;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Boss.AoE
{
    // SIMUL-TeaamProject(hyeonyeop 브랜치) BossAoE/BossCircleAoE.cs 참고 포팅. IDamageable은 AntColony.Core 공용 인터페이스 사용.
    public class BossCircleAoE : MonoBehaviour
    {
        [Header("Telegraph")]
        [SerializeField] private GroundTelegraphCircle telegraphPrefab;
        [SerializeField] private float radius = 4f;
        [SerializeField] private float telegraphDuration = 1.5f;
        [SerializeField] private float telegraphRemainAfterHit = 0.15f;

        [Header("Damage")]
        [SerializeField] private float damage = 25f;
        [SerializeField] private LayerMask targetMask;
        [SerializeField] private float verticalTolerance = 3f;

        public bool IsCasting { get; private set; }

        private void Awake()
        {
            // 인스펙터에서 못 걸어둔 경우, Resources에서 텔레그래프 프리팹을 자동으로 찾아 쓴다.
            if (telegraphPrefab == null)
            {
                telegraphPrefab = Resources.Load<GroundTelegraphCircle>("Telegraph/BossTelegraphCircle");
            }
        }

        public void CastAt(Vector3 center)
        {
            if (!gameObject.activeInHierarchy || IsCasting) return;
            StartCoroutine(CastRoutine(center));
        }

        private IEnumerator CastRoutine(Vector3 center)
        {
            IsCasting = true;

            GroundTelegraphCircle telegraph = null;
            if (telegraphPrefab != null)
            {
                telegraph = Instantiate(telegraphPrefab, center, Quaternion.identity);
                telegraph.SetCenterAndRadius(center, radius);
            }

            yield return new WaitForSeconds(telegraphDuration);

            ResolveHit(center);

            if (telegraph != null)
                Destroy(telegraph.gameObject, telegraphRemainAfterHit);

            IsCasting = false;
        }

        private void ResolveHit(Vector3 center)
        {
            var hits = Physics.OverlapSphere(center, radius, targetMask, QueryTriggerInteraction.Ignore);
            var alreadyDamaged = new HashSet<IDamageable>();

            foreach (var hit in hits)
            {
                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || alreadyDamaged.Contains(damageable)) continue;

                var targetPos = hit.bounds.center;
                var flatDistance = Vector2.Distance(new Vector2(center.x, center.z), new Vector2(targetPos.x, targetPos.z));
                var heightDifference = Mathf.Abs(targetPos.y - center.y);

                if (flatDistance <= radius && heightDifference <= verticalTolerance)
                {
                    alreadyDamaged.Add(damageable);
                    damageable.TakeDamage(damage);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}

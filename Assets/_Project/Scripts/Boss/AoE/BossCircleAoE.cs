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

        public bool IsCasting => cast != null && !cast.hitResolved;
        private BossAoEState cast;
        public BossAoEState Capture() => cast?.Copy();
        private GroundTelegraphCircle telegraph;

        private void OnDisable()
        {
            StopAllCoroutines();
            if (telegraph != null) Destroy(telegraph.gameObject);
            cast = null;
        }

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
            if (!isActiveAndEnabled || IsCasting) return;
            BossTelegraphTiming.Prepare();
            StopAllCoroutines();
            StartCoroutine(CastRoutine(center));
        }

        private IEnumerator CastRoutine(Vector3 center)
        {
            ResetCast();
            cast = new BossAoEState { captured = true, origin = center, forward = Vector3.forward, remaining = telegraphDuration, afterHit = telegraphRemainAfterHit };
            yield return ResumeCast();
        }

        private void ResetCast()
        {
            if (telegraph != null) Destroy(telegraph.gameObject);
            telegraph = null;
            cast = null;
        }

        public void Restore(BossAoEState saved)
        {
            StopAllCoroutines();
            ResetCast();
            if (!BossAoEState.Present(saved) || !isActiveAndEnabled) return;
            cast = saved.Copy();
            StartCoroutine(ResumeCast());
        }

        private IEnumerator ResumeCast()
        {
            if (telegraphPrefab != null)
            {
                telegraph = Instantiate(telegraphPrefab, cast.origin, Quaternion.identity);
                telegraph.SetCenterAndRadius(cast.origin, radius);
            }
            while (cast != null && (!cast.hitResolved || cast.afterHit > 0))
            {
                yield return null;
                if (cast.Advance(Time.deltaTime)) ResolveHit(cast.origin);
            }
            ResetCast();
        }

        private void ResolveHit(Vector3 center)
        {
            var hits = Physics.OverlapSphere(center, Mathf.Sqrt(radius * radius + verticalTolerance * verticalTolerance), targetMask, QueryTriggerInteraction.Ignore);
            var alreadyDamaged = new HashSet<IDamageable>();

            foreach (var hit in hits)
            {
                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || alreadyDamaged.Contains(damageable)) continue;
                // 지상 범위 공격은 공중 유닛에 적용되지 않는다.
                if (CombatTargeting.IsAirborne(damageable)) continue;

                var targetPos = hit.bounds.center;
                var flatDistance = Vector2.Distance(new Vector2(center.x, center.z), new Vector2(targetPos.x, targetPos.z));
                var heightDifference = Mathf.Abs(targetPos.y - center.y);

                if (flatDistance <= radius && heightDifference <= verticalTolerance)
                {
                    alreadyDamaged.Add(damageable);
                    FormationDamage.Apply(damageable, damage, center, area: true);
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

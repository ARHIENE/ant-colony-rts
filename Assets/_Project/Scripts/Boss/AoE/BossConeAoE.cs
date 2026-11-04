using System.Collections;
using System.Collections.Generic;
using AntColony.Boss.Telegraph;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Boss.AoE
{
    // SIMUL-TeaamProject(hyeonyeop 브랜치) BossAoE/BossConeAoE.cs 참고 포팅.
    public class BossConeAoE : MonoBehaviour
    {
        [Header("Telegraph")]
        [SerializeField] private GroundTelegraphSector telegraphPrefab;
        [SerializeField] private float range = 6f;
        [SerializeField] private float angleDeg = 90f;
        [SerializeField] private float telegraphDuration = 1.2f;
        [SerializeField] private float telegraphRemainAfterHit = 0.15f;

        [Header("Damage")]
        [SerializeField] private float damage = 20f;
        [SerializeField] private LayerMask targetMask;
        [SerializeField] private float verticalTolerance = 3f;

        [Header("Ground Snap")]
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float groundCastHeight = 50f;
        [SerializeField] private float groundOffset = 0.05f;

        public bool IsCasting => cast != null && !cast.hitResolved;
        private BossAoEState cast;
        public BossAoEState Capture() => cast?.Copy();
        private GroundTelegraphSector telegraph;

        private void OnDisable()
        {
            StopAllCoroutines();
            if (telegraph != null) Destroy(telegraph.gameObject);
            cast = null;
        }

        private void Awake()
        {
            if (telegraphPrefab == null)
            {
                telegraphPrefab = Resources.Load<GroundTelegraphSector>("Telegraph/BossTelegraphSector");
            }
        }

        public void CastFrom(Vector3 origin, Vector3 forward)
        {
            if (!isActiveAndEnabled || IsCasting) return;
            BossTelegraphTiming.Prepare();

            var flatForward = Vector3.ProjectOnPlane(forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f) flatForward = transform.forward;

            StopAllCoroutines();
            StartCoroutine(CastRoutine(origin, flatForward.normalized));
        }

        private IEnumerator CastRoutine(Vector3 origin, Vector3 flatForward)
        {
            ResetCast();
            cast = new BossAoEState { captured = true, origin = SnapToGround(origin), forward = flatForward, remaining = telegraphDuration, afterHit = telegraphRemainAfterHit };
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
                var rotation = Quaternion.LookRotation(cast.forward, Vector3.up);
                telegraph = Instantiate(telegraphPrefab, cast.origin, rotation);
                telegraph.SetData(cast.origin, rotation, range, angleDeg);
            }
            while (cast != null && (!cast.hitResolved || cast.afterHit > 0))
            {
                yield return null;
                if (cast.Advance(Time.deltaTime)) ResolveHit(cast.origin, cast.forward);
            }
            ResetCast();
        }

        private Vector3 SnapToGround(Vector3 position)
        {
            var rayOrigin = new Vector3(position.x, position.y + groundCastHeight, position.z);
            var rayDistance = groundCastHeight * 2f;

            if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
                return hit.point + hit.normal * groundOffset;

            return position;
        }

        private void ResolveHit(Vector3 origin, Vector3 flatForward)
        {
            var hits = Physics.OverlapSphere(origin, Mathf.Sqrt(range * range + verticalTolerance * verticalTolerance), targetMask, QueryTriggerInteraction.Ignore);
            var alreadyDamaged = new HashSet<IDamageable>();

            foreach (var hit in hits)
            {
                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || alreadyDamaged.Contains(damageable)) continue;
                // 지상 범위 공격은 공중 유닛에 적용되지 않는다.
                if (CombatTargeting.IsAirborne(damageable)) continue;

                var targetPos = hit.bounds.center;
                var toTarget = targetPos - origin;
                var heightDifference = Mathf.Abs(toTarget.y);

                toTarget.y = 0f;
                var flatDistance = toTarget.magnitude;

                if (flatDistance > range) continue;
                if (heightDifference > verticalTolerance) continue;

                if (flatDistance > 0.001f)
                {
                    var angleToTarget = Vector3.Angle(flatForward, toTarget.normalized);
                    if (angleToTarget > angleDeg * 0.5f) continue;
                }

                alreadyDamaged.Add(damageable);
                FormationDamage.Apply(damageable, damage, origin, area: true);
            }
        }
    }
}

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

        public bool IsCasting { get; private set; }

        private void Awake()
        {
            if (telegraphPrefab == null)
            {
                telegraphPrefab = Resources.Load<GroundTelegraphSector>("Telegraph/BossTelegraphSector");
            }
        }

        public void CastFrom(Vector3 origin, Vector3 forward)
        {
            if (!gameObject.activeInHierarchy || IsCasting) return;

            var flatForward = Vector3.ProjectOnPlane(forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f) flatForward = transform.forward;

            StartCoroutine(CastRoutine(origin, flatForward.normalized));
        }

        private IEnumerator CastRoutine(Vector3 origin, Vector3 flatForward)
        {
            IsCasting = true;

            var groundOrigin = SnapToGround(origin);
            var telegraphRotation = Quaternion.LookRotation(flatForward, Vector3.up);

            GroundTelegraphSector telegraph = null;
            if (telegraphPrefab != null)
            {
                telegraph = Instantiate(telegraphPrefab, groundOrigin, telegraphRotation);
                telegraph.SetData(groundOrigin, telegraphRotation, range, angleDeg);
            }

            yield return new WaitForSeconds(telegraphDuration);

            ResolveHit(groundOrigin, flatForward);

            if (telegraph != null)
                Destroy(telegraph.gameObject, telegraphRemainAfterHit);

            IsCasting = false;
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
            var hits = Physics.OverlapSphere(origin, range, targetMask, QueryTriggerInteraction.Ignore);
            var alreadyDamaged = new HashSet<IDamageable>();

            foreach (var hit in hits)
            {
                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || alreadyDamaged.Contains(damageable)) continue;

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
                damageable.TakeDamage(damage);
            }
        }
    }
}

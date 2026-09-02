using AntColony.Boss.AoE;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Boss
{
    // SIMUL-TeaamProject(hyeonyeop 브랜치) BossBasicPatternLoop.cs 참고 포팅. 단일 패턴(원형 AoE) 반복.
    public class BossBasicPatternLoop : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BossCircleAoE circleAttack;

        [Header("Target Search")]
        [SerializeField] private LayerMask antLayerMask;
        [SerializeField] private float searchRadius = 30f;
        [SerializeField] private float searchInterval = 0.2f;

        [Header("Timing")]
        [SerializeField] private float firstCastDelay = 1.5f;
        [SerializeField] private float castCooldown = 3.0f;

        [Header("Targeting")]
        [SerializeField] private bool faceTarget = true;
        [SerializeField] private bool castOnTargetPosition = true;
        [SerializeField] private float forwardCastDistance = 4f;

        private float castTimer;
        private float searchTimer;
        private Transform currentTarget;

        private void Start()
        {
            castTimer = firstCastDelay;
            searchTimer = 0f;
        }

        private void Update()
        {
            if (circleAttack == null) return;

            searchTimer -= Time.deltaTime;
            if (searchTimer <= 0f)
            {
                currentTarget = FindNearestAntTarget();
                searchTimer = searchInterval;
            }

            if (currentTarget == null) return;

            if (faceTarget) FaceTargetFlat(currentTarget.position);

            if (circleAttack.IsCasting) return;

            castTimer -= Time.deltaTime;
            if (castTimer > 0f) return;

            var castPosition = castOnTargetPosition
                ? currentTarget.position
                : transform.position + transform.forward * forwardCastDistance;

            circleAttack.CastAt(castPosition);
            castTimer = castCooldown;
        }

        private Transform FindNearestAntTarget()
        {
            var hits = Physics.OverlapSphere(transform.position, searchRadius, antLayerMask, QueryTriggerInteraction.Ignore);

            Transform nearest = null;
            var nearestSqrDist = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit == null) continue;
                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null) continue;

                var target = hit.transform.root;
                var diff = target.position - transform.position;
                diff.y = 0f;

                var sqrDist = diff.sqrMagnitude;
                if (sqrDist < nearestSqrDist)
                {
                    nearestSqrDist = sqrDist;
                    nearest = target;
                }
            }

            return nearest;
        }

        private void FaceTargetFlat(Vector3 targetPosition)
        {
            var dir = targetPosition - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;

            var lookRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 8f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, searchRadius);
        }
    }
}

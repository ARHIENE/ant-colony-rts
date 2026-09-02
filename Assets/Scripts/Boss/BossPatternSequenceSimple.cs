using AntColony.Boss.AoE;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Boss
{
    // SIMUL-TeaamProject(hyeonyeop 브랜치) BossPatternSequenceSimple.cs 참고 포팅. 원형→부채꼴→직선 순환 패턴.
    public class BossPatternSequenceSimple : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BossCircleAoE circleAttack;
        [SerializeField] private BossConeAoE coneAttack;
        [SerializeField] private BossLineAoE lineAttack;

        [Header("Target Search")]
        [SerializeField] private LayerMask antLayerMask;
        [SerializeField] private float searchRadius = 30f;
        [SerializeField] private float searchInterval = 0.2f;

        [Header("Timing")]
        [SerializeField] private float firstCastDelay = 1.5f;
        [SerializeField] private float castCooldown = 2.5f;

        [Header("Facing")]
        [SerializeField] private bool faceTarget = true;
        [SerializeField] private float rotateSpeedDegPerSec = 360f;

        private float castTimer;
        private float searchTimer;
        private int nextPatternIndex;
        private Transform currentTarget;

        private void Start()
        {
            castTimer = firstCastDelay;
            searchTimer = 0f;
        }

        private void Update()
        {
            searchTimer -= Time.deltaTime;
            if (searchTimer <= 0f)
            {
                currentTarget = FindNearestAntTarget();
                searchTimer = searchInterval;
            }

            if (currentTarget == null) return;

            var isCasting = AnyCasting();

            if (!isCasting && faceTarget) FaceTargetFlat(currentTarget.position);
            if (isCasting) return;

            castTimer -= Time.deltaTime;
            if (castTimer > 0f) return;

            TryCastNextPattern();
            castTimer = castCooldown;
        }

        private bool AnyCasting()
        {
            return (circleAttack != null && circleAttack.IsCasting)
                || (coneAttack != null && coneAttack.IsCasting)
                || (lineAttack != null && lineAttack.IsCasting);
        }

        private void TryCastNextPattern()
        {
            for (var i = 0; i < 3; i++)
            {
                var pattern = (nextPatternIndex + i) % 3;
                if (TryCastPattern(pattern))
                {
                    nextPatternIndex = (pattern + 1) % 3;
                    return;
                }
            }
        }

        private bool TryCastPattern(int pattern)
        {
            switch (pattern)
            {
                case 0:
                    if (circleAttack != null && currentTarget != null)
                    {
                        circleAttack.CastAt(currentTarget.position);
                        return true;
                    }
                    break;

                case 1:
                    if (coneAttack != null)
                    {
                        coneAttack.CastFrom(transform.position, transform.forward);
                        return true;
                    }
                    break;

                case 2:
                    if (lineAttack != null)
                    {
                        lineAttack.CastFrom(transform.position, transform.forward);
                        return true;
                    }
                    break;
            }

            return false;
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

            var targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotateSpeedDegPerSec * Time.deltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, searchRadius);
        }
    }
}

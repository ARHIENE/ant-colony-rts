using UnityEngine;

namespace AntColony.Units
{
    public sealed partial class AntVisual
    {
        private Vector3 groundPosition;
        private float nextGroundCheck;
        private bool groundKnown;

        private void AlignToGround()
        {
            if (!kemono || commander != null && commander.IsEmbarked) return;
            if (soldier != null && soldier.IsFlying || enemy != null && enemy.IsFlying)
            { ApplyGroundOffset(0); groundKnown = false; return; }
            var position = transform.position;
            if (groundKnown && (position - groundPosition).sqrMagnitude < .0025f) return;
            // Grounded agents retain the old capsule/base offset. Only the display follows the actual ground surface.
            if (groundKnown && Mathf.Abs(position.y - groundPosition.y) < .01f && Time.unscaledTime < nextGroundCheck) return;
            nextGroundCheck = Time.unscaledTime + .1f;
            if (!Physics.Raycast(position + Vector3.up * 4, Vector3.down, out var hit, 12, 1 << 8, QueryTriggerInteraction.Ignore)) return;
            groundPosition = position; groundKnown = true;
            ApplyGroundOffset(transform.InverseTransformPoint(new Vector3(position.x, hit.point.y, position.z)).y);
        }

        private void ApplyGroundOffset(float offset)
        {
            var old = model.transform.localPosition;
            if (Mathf.Abs(old.y - offset) < .001f) return;
            model.transform.localPosition = new Vector3(old.x, offset, old.z);
            if (ring != null) ring.transform.localPosition = Vector3.up * offset;
            if (TryGetComponent<CapsuleCollider>(out var capsule)) capsule.center += Vector3.up * (offset - old.y);
        }
    }
}

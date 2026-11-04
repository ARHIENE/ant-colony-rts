using UnityEngine;

namespace AntColony.Units
{
    public sealed class SelectionOutline : MonoBehaviour
    {
        private LineRenderer line;
        private Bounds bounds;
        private Vector3 offset;
        private Collider source;
        public void Show(bool selected)
        {
            if (line == null && selected)
            {
                source = GetComponent<Collider>();
                bounds = source != null ? source.bounds : new Bounds(transform.position, Vector3.one * 2.6f);
                offset = bounds.center - transform.position;
                var child = new GameObject("Selected Outline"); child.transform.SetParent(transform, false);
                line = child.AddComponent<LineRenderer>(); line.useWorldSpace = true; line.loop = true;
                line.positionCount = 4; line.widthMultiplier = .16f;
                line.sharedMaterial = Resources.Load<Material>("AntSelection");
                line.startColor = line.endColor = Color.white;
            }
            if (line == null) return;
            line.enabled = selected;
            if (selected) LateUpdate();
        }
        private void LateUpdate()
        {
            if (line == null || !line.enabled) return;
            if (source != null) { bounds = source.bounds; offset = bounds.center - transform.position; }
            var center = transform.position + offset;
            center.y -= bounds.extents.y - .08f;
            var x = bounds.extents.x + .18f; var z = bounds.extents.z + .18f;
            line.SetPosition(0, center + new Vector3(-x, 0, -z));
            line.SetPosition(1, center + new Vector3(-x, 0, z));
            line.SetPosition(2, center + new Vector3(x, 0, z));
            line.SetPosition(3, center + new Vector3(x, 0, -z));
        }
    }
}

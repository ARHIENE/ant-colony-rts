using System.Linq;
using UnityEngine;

namespace AntColony.Buildings
{
    // Meshy 원본에서 분리한 문짝의 경첩을 움직인다. 통과/차단과 저장은 Gate가 담당한다.
    public sealed class MeshyGateVisual : MonoBehaviour
    {
        private Gate gate;
        private Transform left, right;
        private Quaternion leftClosed, rightClosed;
        private void Start()
        {
            gate = GetComponentInParent<Gate>();
            var parts = GetComponentsInChildren<Transform>();
            left = parts.FirstOrDefault(t => t.name == "GateLeafLeft");
            right = parts.FirstOrDefault(t => t.name == "GateLeafRight");
            if (!gate || !left || !right) { enabled = false; return; }
            leftClosed = left.localRotation; rightClosed = right.localRotation;
            Apply(true);
        }
        private void LateUpdate() { if (gate && left && right) Apply(false); }
        private void Apply(bool immediately)
        {
            var l = Quaternion.AngleAxis(gate.Open ? -100 : 0, Vector3.up) * leftClosed;
            var r = Quaternion.AngleAxis(gate.Open ? 100 : 0, Vector3.up) * rightClosed;
            left.localRotation = immediately ? l : Quaternion.RotateTowards(left.localRotation, l, 240 * Time.unscaledDeltaTime);
            right.localRotation = immediately ? r : Quaternion.RotateTowards(right.localRotation, r, 240 * Time.unscaledDeltaTime);
        }
    }
}

using UnityEngine;

namespace AntColony.Buildings
{
    // 기존 미리보기와 함께 복제되어 연속 배치 각 칸도 같은 기호를 쓴다.
    public sealed class PlacementPreviewSymbol : MonoBehaviour
    {
        private LineRenderer first, second;
        public static string Symbol(bool valid) => valid ? "+" : "X";
        public static Vector3 Point(bool valid, int stroke, int end)
        {
            float sign = end == 0 ? -0.4f : 0.4f;
            return valid ? (stroke == 0 ? new Vector3(sign, .52f, 0) : new Vector3(0, .52f, sign))
                : new Vector3(sign, .52f, stroke == 0 ? sign : -sign);
        }
        private void Awake()
        {
            first = Stroke("Placement Symbol A"); second = Stroke("Placement Symbol B");
        }
        private LineRenderer Stroke(string name)
        {
            var existing = transform.Find(name);
            if (existing != null) return existing.GetComponent<LineRenderer>();
            var child = new GameObject(name); child.transform.SetParent(transform, false);
            var line = child.AddComponent<LineRenderer>(); line.useWorldSpace = false;
            line.positionCount = 2; line.widthMultiplier = .12f;
            line.sharedMaterial = Resources.Load<Material>("AntSelection");
            line.startColor = line.endColor = Color.white;
            return line;
        }
        public void Refresh(bool valid)
        {
            for (int end = 0; end < 2; end++)
            {
                first.SetPosition(end, Point(valid, 0, end));
                second.SetPosition(end, Point(valid, 1, end));
            }
        }
    }
}

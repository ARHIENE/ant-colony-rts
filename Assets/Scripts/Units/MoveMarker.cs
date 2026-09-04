using UnityEngine;

namespace AntColony.Units
{
    // 우클릭으로 이동 명령을 내린 지점에 잠깐 표시되는 확인 마커(스타크래프트류).
    // 프리팹 없이 원기둥 프리미티브를 런타임에 생성해 얇은 원판처럼 만들고, 줄어들며 사라진다.
    public class MoveMarker : MonoBehaviour
    {
        private const float DiscHeight = 0.02f;

        private float lifetime;
        private float timer;
        private float startScale;
        private float endScale;

        public static void Spawn(Vector3 groundPoint, Color color, float lifetime = 0.4f, float startScale = 1.4f, float endScale = 0.3f)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "MoveMarker";

            var collider = visual.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            var renderer = visual.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(renderer.sharedMaterial) { color = color };

            visual.transform.position = groundPoint + Vector3.up * 0.05f;
            visual.transform.localScale = new Vector3(startScale, DiscHeight, startScale);

            var marker = visual.AddComponent<MoveMarker>();
            marker.lifetime = lifetime;
            marker.startScale = startScale;
            marker.endScale = endScale;

            Destroy(visual, lifetime);
        }

        private void Update()
        {
            timer += Time.deltaTime;
            var t = Mathf.Clamp01(timer / lifetime);
            var scale = Mathf.Lerp(startScale, endScale, t);
            transform.localScale = new Vector3(scale, DiscHeight, scale);
        }
    }
}

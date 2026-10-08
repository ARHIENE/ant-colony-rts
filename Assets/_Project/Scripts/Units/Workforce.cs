using UnityEngine;

namespace AntColony.Units
{
    // 장수 작업 대상 표식. 평시 작업에 일반개미 지원은 없다(2026-10-08 기획 폐기).
    public sealed class Workforce : MonoBehaviour
    {
        public static Workforce For(Component target) => target == null ? null
            : target.GetComponent<Workforce>() ?? target.gameObject.AddComponent<Workforce>();
    }
}

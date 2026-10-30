using System;
using UnityEngine;

namespace AntColony.Boss.AoE
{
    // 경고와 명중 뒤 표시를 같은 게임 시간으로 보존한다. 피해 판정 전에 hitResolved를 세운다.
    [Serializable]
    public sealed class BossAoEState
    {
        public Vector3 origin, forward;
        public float remaining, afterHit;
        public bool captured, hitResolved;
        public BossAoEState Copy() => (BossAoEState)MemberwiseClone();
        public static bool Present(BossAoEState s) => s != null && s.captured;

        public bool Advance(float seconds)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds)) return false;
            if (hitResolved) { afterHit = Math.Max(0, afterHit - seconds); return false; }
            remaining = Math.Max(0, remaining - seconds);
            if (remaining > 0) return false;
            hitResolved = true;
            return true;
        }

        public static bool Valid(BossAoEState s)
        {
            bool N(float n) => !float.IsNaN(n) && !float.IsInfinity(n) && n >= 0 && n <= 60;
            bool V(Vector3 v) => !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z)
                && Math.Abs(v.x) <= 1000000 && Math.Abs(v.y) <= 1000000 && Math.Abs(v.z) <= 1000000;
            if (s == null) return true;
            // JsonUtility가 null/없는 인라인 객체를 0으로 만들 때 구저장과 유휴 상태를 유지한다.
            if (!s.captured) return s.origin.x == 0 && s.origin.y == 0 && s.origin.z == 0
                && s.forward.x == 0 && s.forward.y == 0 && s.forward.z == 0 && s.remaining == 0 && s.afterHit == 0 && !s.hitResolved;
            return V(s.origin) && V(s.forward) && N(s.remaining) && N(s.afterHit)
                && Math.Abs(s.forward.y) < .001f && Math.Abs(s.forward.x * s.forward.x + s.forward.z * s.forward.z - 1) < .001f
                && (!s.hitResolved || s.remaining == 0);
        }
    }
}

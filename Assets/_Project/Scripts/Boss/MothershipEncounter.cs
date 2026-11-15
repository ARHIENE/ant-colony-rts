using System;
using System.Linq;
using AntColony.Boss.Telegraph;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Boss
{
    // The final encounter shares the ordinary damage and ground-warning rendering paths.
    public sealed class MothershipEncounter : MonoBehaviour
    {
        [Serializable] public sealed class State
        {
            public int nextPattern, casting = -1;
            public float remaining = 3;
            public Vector3 origin, forward = Vector3.forward;
        }
        State state = new State();
        BossHealth boss;
        Animator animator;
        GameObject warning;
        public int Phase => boss != null && boss.CurrentHp <= boss.MaxHp * .5f ? 2 : 1;
        public string Status => state.casting < 0 ? $"{Phase}단계 · 다음 공격 {state.remaining:0.0}초"
            : $"{Phase}단계 · {(state.casting == 0 ? "집중 포격 · 원 밖으로 이동" : state.casting == 1 ? "관통 광선 · 옆으로 회피" : "근접 충격파 · 지휘체에서 이탈")} · {state.remaining:0.0}초";
        void Awake() { boss = GetComponent<BossHealth>(); animator = GetComponentInChildren<Animator>(); }
        void Update() => Tick(Time.deltaTime);
        public void Tick(float seconds)
        {
            var campaign = RocketCampaign.Instance;
            if (boss == null || boss.IsDead || campaign == null || AntColony.Save.SaveSystem.Busy || !(seconds > 0) || float.IsInfinity(seconds)) return;
            var crew = campaign.MothershipCrew.Where(c => c != null && !c.IsDead && !c.IsEmbarked && c.PersonalHealth > 0
                && Vector3.ProjectOnPlane(c.Position - boss.Position, Vector3.up).sqrMagnitude <= 32 * 32).ToArray();
            if (crew.Length == 0)
            {
                state.casting = -1; state.remaining = Mathf.Max(state.remaining, 3); ClearWarning(); Animate(0); return;
            }
            if (state.casting >= 0) ShowWarning();
            state.remaining = Mathf.Max(0, state.remaining - seconds);
            if (state.remaining > 0) return;
            if (state.casting < 0)
            {
                var target = crew.OrderBy(c => (c.Position - boss.Position).sqrMagnitude).First();
                state.casting = state.nextPattern;
                state.nextPattern = (state.nextPattern + 1) % (Phase == 2 ? 3 : 2);
                state.origin = state.casting == 0 ? target.Position : boss.Position;
                state.origin.y = RocketCampaign.ShipLanding.y + .06f;
                state.forward = Vector3.ProjectOnPlane(target.Position - boss.Position, Vector3.up).normalized;
                if (state.forward.sqrMagnitude < .5f) state.forward = Vector3.back;
                // Default foot movement is 1.8m/s. Include acceleration and input time:
                // a 5m targeted circle cannot be escaped in the former 2.5s window.
                state.remaining = state.casting == 0 ? 4f : state.casting == 1 ? 3f : 4.5f;
                transform.rotation = Quaternion.LookRotation(state.forward);
                Animate(2);
                AoE.BossTelegraphTiming.Prepare(); ShowWarning();
                return;
            }
            foreach (var c in crew)
                if (Contains(state, c.Position)) FormationDamage.Apply(c, Phase == 2 ? 22 : 16, state.origin, ranged: true, area: true);
            state.casting = -1; state.remaining = Phase == 2 ? 2.5f : 4;
            Animate(0);
            ClearWarning();
        }
        public static bool Contains(State s, Vector3 point)
        {
            var delta = point - s.origin; delta.y = 0;
            if (s.casting == 1)
            {
                float along = Vector3.Dot(delta, s.forward);
                return along >= 0 && along <= 60 && Mathf.Abs(Vector3.Dot(delta, Vector3.Cross(Vector3.up, s.forward))) <= 2;
            }
            return s.casting >= 0 && delta.sqrMagnitude <= (s.casting == 0 ? 25 : 64);
        }
        void ShowWarning()
        {
            if (warning != null || state.casting < 0) return;
            if (state.casting == 1)
            {
                var prefab = Resources.Load<GroundTelegraphLine>("Telegraph/BossTelegraphLine");
                if (prefab == null) return;
                var line = Instantiate(prefab); line.SetData(state.origin, Quaternion.LookRotation(state.forward), 60, 4); warning = line.gameObject;
            }
            else
            {
                var prefab = Resources.Load<GroundTelegraphCircle>("Telegraph/BossTelegraphCircle");
                if (prefab == null) return;
                var circle = Instantiate(prefab); circle.SetCenterAndRadius(state.origin, state.casting == 0 ? 5 : 8); warning = circle.gameObject;
            }
        }
        void ClearWarning() { if (warning != null) Destroy(warning); warning = null; }
        void Animate(int action) { if (animator != null) animator.SetInteger("Action", action); }
        void OnDisable() => ClearWarning();
        public State Capture() => JsonUtility.FromJson<State>(JsonUtility.ToJson(state));
        public void Restore(State saved)
        {
            if (!Valid(saved)) throw new FormatException("Invalid mothership encounter");
            state = saved == null ? new State() : JsonUtility.FromJson<State>(JsonUtility.ToJson(saved));
            ClearWarning(); if (isActiveAndEnabled) ShowWarning();
        }
        public static bool Valid(State s) => s == null || s.nextPattern >= 0 && s.nextPattern < 3 && s.casting >= -1 && s.casting < 3
            && Finite(s.remaining) && s.remaining >= 0 && s.remaining <= 10
            && Finite(s.origin.sqrMagnitude) && Finite(s.forward.sqrMagnitude)
            && (s.casting != 1 || Mathf.Abs(s.forward.sqrMagnitude - 1) < .01f && Mathf.Abs(s.forward.y) < .01f);
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

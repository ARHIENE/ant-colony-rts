using System;
using System.Linq;
using AntColony.Boss.Telegraph;
using AntColony.Core;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Boss
{
    // 기존 지상 AoE는 그대로 둔다. 비행 공격만 대공 가능하며 착지 중 근접 공략도 유지한다.
    public sealed class BossEncounter : MonoBehaviour
    {
        [Serializable] public sealed class State
        {
            public bool taken; // JsonUtility가 null을 빈 객체로 만들 때 구저장/비보스와 구분한다.
            public float elapsed = BossEncounterRules.FlightSeconds, pulseRemaining = BossEncounterRules.CycleSeconds - BossEncounterRules.FlightSeconds, warningRemaining;
            public bool warning;
            public Vector3 strike;
        }
        private State state = new State { taken = true };
        private BossHealth boss;
        private BossId id;
        private Vector3 anchor;
        private GroundTelegraphCircle telegraph;
        public bool Airborne => BossEncounterRules.Flying(state.elapsed);
        public void Initialize(BossHealth value, BossId bossId)
        {
            boss = value; id = bossId; anchor = value.Position;
            boss.ConfigureCampaign(bossId);
        }
        private void Update() => Tick(Time.deltaTime);
        public void Tick(float seconds)
        {
            if (AntColony.Save.SaveSystem.Busy || boss == null || boss.IsDead || !(seconds > 0) || float.IsInfinity(seconds)) return;
            var target = AntUnitBase.Active.OfType<CommanderAnt>().Where(c => c.IsColonyMember && !c.IsDead && !c.IsEmbarked
                && !c.IsCaptive && c.PersonalHealth > 0 && (c.Position - anchor).sqrMagnitude <= 35 * 35)
                .OrderBy(c => (c.Position - anchor).sqrMagnitude).FirstOrDefault();
            if (target == null && !state.warning) return;
            state.elapsed = (state.elapsed + seconds) % BossEncounterRules.CycleSeconds;
            transform.position = anchor + (Airborne ? Vector3.up * 3 : Vector3.zero);
            if (state.warning)
            {
                ShowTelegraph();
                state.warningRemaining = Mathf.Max(0, state.warningRemaining - seconds);
                if (state.warningRemaining > 0) return;
                state.warning = false; ClearTelegraph();
                foreach (var c in AntUnitBase.Active.OfType<CommanderAnt>().ToArray())
                {
                    if (!c.IsColonyMember || c.IsDead || c.IsEmbarked || c.IsCaptive || c.PersonalHealth <= 0) continue;
                    var delta = c.Position - state.strike; delta.y = 0;
                    if (delta.sqrMagnitude > BossEncounterRules.StrikeRadius * BossEncounterRules.StrikeRadius) continue;
                    FormationDamage.Apply(c, 12, anchor, ranged: true, area: true);
                    if (BossEncounterRules.Heat(id) > 0) c.TakeHeatDamage(BossEncounterRules.Heat(id));
                }
                SpawnAdds();
                return;
            }
            state.pulseRemaining = Mathf.Max(0, state.pulseRemaining - seconds);
            if (state.pulseRemaining > 0 || target == null) return;
            state.elapsed = 0; // 비행 시작과 예고를 일치시킨다. 재등장·로드에서도 같은 상태를 사용한다.
            state.warning = true; state.warningRemaining = BossEncounterRules.WarningSeconds;
            state.pulseRemaining = BossEncounterRules.CycleSeconds - BossEncounterRules.WarningSeconds;
            state.strike = target.Position; state.strike.y = anchor.y - 1;
            AoE.BossTelegraphTiming.Prepare(); ShowTelegraph();
        }
        private void SpawnAdds()
        {
            var site = GetComponentInParent<ExpeditionSite>();
            if (site == null) return;
            int existing = site.GetComponentsInChildren<WildMonster>().Count(m => !m.IsDead && m.name == "보스 호위");
            for (int i = existing; i < BossEncounterRules.AddLimit(id); i++)
            {
                var point = anchor + new Vector3((i - 1.5f) * 2, 0, -3);
                if (!NavMesh.SamplePosition(point, out var hit, 5, NavMesh.AllAreas)) continue;
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.SetActive(false); go.name = "보스 호위"; go.transform.SetParent(site.transform);
                go.transform.position = hit.position; go.transform.localScale = Vector3.one * .5f;
                var guard = go.AddComponent<WildMonster>(); guard.ConfigureBossGuard((int)id, CampaignResearch.Instance?.BossProgress.Cycle(id) ?? 0); go.SetActive(true);
            }
        }
        private void ShowTelegraph()
        {
            if (telegraph != null) return;
            var prefab = Resources.Load<GroundTelegraphCircle>("Telegraph/BossTelegraphCircle");
            if (prefab == null) return;
            telegraph = Instantiate(prefab, state.strike, Quaternion.identity);
            telegraph.SetCenterAndRadius(state.strike, BossEncounterRules.StrikeRadius);
        }
        private void ClearTelegraph() { if (telegraph != null) Destroy(telegraph.gameObject); }
        private void OnDisable() => ClearTelegraph();
        internal void ResetEncounter() { state = new State { taken = true }; transform.position = anchor; ClearTelegraph(); }
        public State Capture() => JsonUtility.FromJson<State>(JsonUtility.ToJson(state));
        public void Restore(State value)
        {
            if (!Validate(value)) throw new FormatException("Invalid boss encounter state.");
            state = !Present(value) ? new State { taken = true } : JsonUtility.FromJson<State>(JsonUtility.ToJson(value));
            transform.position = anchor + (Airborne ? Vector3.up * 3 : Vector3.zero); ClearTelegraph();
        }
        public static bool Present(State s) => s != null && s.taken;
        public static bool Validate(State s) => !Present(s) || RocketSnapshot.Finite(s.elapsed) && s.elapsed <= BossEncounterRules.CycleSeconds
            && RocketSnapshot.Finite(s.pulseRemaining) && s.pulseRemaining <= BossEncounterRules.CycleSeconds
            && RocketSnapshot.Finite(s.warningRemaining) && s.warningRemaining <= BossEncounterRules.WarningSeconds
            && s.warning == (s.warningRemaining > 0)
            && !float.IsNaN(s.strike.sqrMagnitude) && !float.IsInfinity(s.strike.sqrMagnitude);
    }
}

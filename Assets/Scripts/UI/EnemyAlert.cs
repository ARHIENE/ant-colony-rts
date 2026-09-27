using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Map;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.UI
{
    // 적 발견 경보(스타크래프트의 기지 공격 알림). 본거지에 새 적대 유닛이 나타나거나 기존 적이 전투를 시작하면
    // 위기 토스트를 걸고 경보음 훅을 부른다. 경보한 적이 모두 사라지면 위기 토스트를 내린다.
    public sealed class EnemyAlert : MonoBehaviour
    {
        public const float SoundCooldown = 10f;
        // 경보음 에셋이 없어 비워 둔다. 클립을 넣으면 그대로 재생된다.
        public static AudioClip AlarmClip;
        public static event Action Alarm;
        public static int AlarmCount { get; private set; }
        private static EnemyAlert instance;
        private readonly HashSet<Component> known = new HashSet<Component>(), fighting = new HashSet<Component>(), alerted = new HashSet<Component>();
        private bool seeded;
        private float scan, lastSound = -999;
        private AudioSource source;

        public static bool CrisisActive => instance != null && instance.alerted.Count > 0;
        private void Awake() { instance = this; source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; }
        private void OnDestroy() { if (instance == this) { instance = null; ToastManager.SetCrisis("enemy", null); } }

        private void Update()
        {
            if (SaveSystem.Busy || !GameSession.Exists || !GameSession.Instance.GameStarted) { seeded = false; return; }
            scan -= Time.unscaledDeltaTime; if (scan > 0) return; scan = .5f;
            Scan();
        }

        // 검사용으로도 쓴다. 시작·불러오기 직후 첫 스캔은 이미 있던 적을 경보 없이 기억만 한다.
        public void Scan()
        {
            var threats = Threats().ToList();
            var raised = false;
            foreach (var t in threats)
            {
                var inCombat = t is WildMonster m ? m.InCombat : t is CommanderAnt c && c.IsInCombat;
                if (seeded && (!known.Contains(t) || inCombat && !fighting.Contains(t))) { alerted.Add(t); raised = true; }
                if (inCombat) fighting.Add(t); else fighting.Remove(t);
            }
            known.Clear(); known.UnionWith(threats);
            alerted.RemoveWhere(t => t == null || !known.Contains(t));
            fighting.RemoveWhere(t => t == null || !known.Contains(t));
            seeded = true;
            ToastManager.SetCrisis("enemy", alerted.Count > 0 ? $"적 발견: 본거지에 적 {alerted.Count} — 징집소에서 출전하세요." : null);
            if (!raised) return;
            AlarmCount++; Alarm?.Invoke(); FirstHints.Trigger("invasion");
            if (AlarmClip != null && Time.unscaledTime - lastSound >= SoundCooldown) { source.PlayOneShot(AlarmClip); lastSound = Time.unscaledTime; }
        }

        private static IEnumerable<Component> Threats()
        {
            var home = HomeMapBuilder.CurrentWorldBounds;
            bool AtHome(Vector3 p) => home.Contains(new Vector3(p.x, 0, p.z));
            foreach (var m in WildMonster.All)
                if (m != null && CombatTargeting.CanAttack(UnitRole.Ranged, m) && AtHome(m.Position)) yield return m;
            foreach (var u in AntUnitBase.Active)
                if (u is CommanderAnt c && c.IsHostile && CombatTargeting.CanAttack(UnitRole.Ranged, c) && AtHome(c.Position)) yield return c;
        }
    }
}

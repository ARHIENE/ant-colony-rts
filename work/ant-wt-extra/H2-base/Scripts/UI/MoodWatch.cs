using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;

namespace AntColony.UI
{
    // 정신적 붕괴: 기분 위험 구간(≤35) 진입 시 경고 토스트, 붕괴 중에는 장수 주위에 색 오라.
    public sealed class MoodWatch : MonoBehaviour
    {
        public static readonly Color AuraColor = new Color(.85f, .3f, .95f);
        private readonly Dictionary<CommanderAnt, int> warned = new Dictionary<CommanderAnt, int>();
        private readonly Dictionary<CommanderAnt, LineRenderer> auras = new Dictionary<CommanderAnt, LineRenderer>();
        private float scan;

        public static bool HasAura(CommanderAnt c) => c != null && c.transform.Find("BreakAura") is Transform t && t.gameObject.activeSelf;

        private void Update()
        {
            if (SaveSystem.Busy || CommanderRoster.Instance == null) { warned.Clear(); return; }
            scan -= Time.unscaledDeltaTime; if (scan > 0) return; scan = .5f;
            Scan();
        }

        public void Scan()
        {
            foreach (var c in warned.Keys.Where(c => c == null).ToArray()) warned.Remove(c);
            foreach (var c in CommanderRoster.Instance.Commanders)
            {
                if (c == null) continue;
                var member = c.IsColonyMember && !c.IsHostile;
                var alert = member && CommanderOverhead.MoodAlert(c);
                var level = GameBalanceNeeds.MoodWarning(c.Mood);
                if (alert && level > 0 && (!warned.TryGetValue(c, out var previous) || previous != level)
                    && GameSession.Exists && GameSession.Instance.GameStarted)
                {
                    warned[c] = level;
                    var cause = c.PersonalState.moodFactors.Where(f => f.value < 0).OrderBy(f => f.value).FirstOrDefault()?.reason ?? "부정 요인 없음";
                    var remaining = Mathf.Max(0, SocialRules.Month - c.PersonalState.lowMoodSeconds);
                    ToastManager.Show($"기분 {new[] { "", "주의", "위험", "위급" }[level]}: {c.CommanderName} ({c.Mood:0}) — {cause} · 이탈까지 {remaining:0}초", ToastKind.Warning);
                    FirstHints.Trigger("collapse");
                }
                else if (!alert || level == 0) warned.Remove(c);
                Aura(c, member && c.PersonalState.mentalBreak != MentalBreak.None);
            }
        }

        private void Aura(CommanderAnt c, bool on)
        {
            if (!auras.TryGetValue(c, out var ring) || ring == null)
            {
                if (!on) return;
                var go = new GameObject("BreakAura"); go.transform.SetParent(c.transform, false);
                ring = go.AddComponent<LineRenderer>(); ring.useWorldSpace = false; ring.loop = true;
                ring.positionCount = 40; ring.widthMultiplier = .12f;
                ring.sharedMaterial = Resources.Load<Material>("AntSelection");
                var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", AuraColor); ring.SetPropertyBlock(block);
                for (var i = 0; i < ring.positionCount; i++)
                {
                    var angle = i * Mathf.PI * 2 / ring.positionCount;
                    ring.SetPosition(i, new Vector3(Mathf.Cos(angle), .08f, Mathf.Sin(angle)) * 1.5f);
                }
                auras[c] = ring;
            }
            ring.gameObject.SetActive(on);
        }
    }
}

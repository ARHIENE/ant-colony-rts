using System.Collections.Generic;
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
        private readonly HashSet<CommanderAnt> warned = new HashSet<CommanderAnt>();
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
            warned.RemoveWhere(c => c == null);
            foreach (var c in CommanderRoster.Instance.Commanders)
            {
                if (c == null) continue;
                var member = c.IsColonyMember && !c.IsHostile;
                var alert = member && CommanderOverhead.MoodAlert(c);
                if (alert && warned.Add(c) && GameSession.Exists && GameSession.Instance.GameStarted)
                {
                    ToastManager.Show($"기분 경고: {c.CommanderName} (기분 {c.Mood:0}) — 정신 붕괴 위험", ToastKind.Warning);
                    FirstHints.Trigger("collapse");
                }
                else if (!alert) warned.Remove(c);
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

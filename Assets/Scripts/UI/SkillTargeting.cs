using System.Collections.Generic;
using System.Linq;
using AntColony.Units;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace AntColony.UI
{
    [DefaultExecutionOrder(-100)]
    public sealed class SkillTargeting : MonoBehaviour
    {
        private static SkillTargeting instance;
        private readonly List<CommanderAnt> pending = new List<CommanderAnt>();
        private bool wings;
        private int consumedFrame = -1;
        public static bool ConsumesPointerInput => instance != null && (instance.pending.Count > 0 || instance.consumedFrame == Time.frameCount);
        private void Awake() => instance = this;
        private void OnDestroy() { if (instance == this) instance = null; }
        public static void Begin(IEnumerable<CommanderAnt> commanders, bool wingSkill)
        {
            if (instance == null) return;
            instance.pending.Clear(); instance.wings = wingSkill;
            foreach (var c in commanders.Where(c => c != null).Distinct())
                if (wingSkill ? c.CanDive : c.CanAcidRain) instance.pending.Add(c);
                else if (!wingSkill) { if (c.CanPowerStrike) c.TryPowerStrike(); else if (c.CanDefensiveStance) c.TryDefensiveStance(); else c.TryRally(); }
            if (instance.pending.Count > 0) ToastManager.Show(wingSkill ? "급강하: 지면 클릭, 우클릭 취소" : "산성비: 지면 클릭, 우클릭 취소");
        }
        private void Update()
        {
            if (pending.Count == 0) return;
            if (GameMenuController.BlocksInput) { pending.Clear(); return; }
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (mouse.rightButton.wasPressedThisFrame || Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            { pending.Clear(); consumedFrame = Time.frameCount; return; }
            if (!mouse.leftButton.wasPressedThisFrame || EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            consumedFrame = Time.frameCount;
            var camera = UnityEngine.Camera.main;
            if (camera == null || !Physics.Raycast(camera.ScreenPointToRay(mouse.position.ReadValue()), out var hit, 2000, 1 << 8)) return;
            foreach (var c in pending) if (c != null) { if (wings) c.TryDive(hit.point); else c.TryAcidRain(hit.point); }
            pending.Clear();
        }
    }
}

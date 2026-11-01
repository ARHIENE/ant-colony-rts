using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Units;
using UnityEngine.InputSystem;

namespace AntColony.UI
{
    // 부대 지정(2026-11-01, StarCraft): Ctrl+1~9 지정, 1~9 다시 선택, 같은 숫자 두 번이면 카메라 이동.
    // 마침표(.)는 할 일 없는 장수를 차례로 선택(RimWorld 유휴 개척민 알림). 저장하지 않는다(한 판 동안만).
    public static class ControlGroups
    {
        private static readonly Dictionary<int, List<SelectableObject>> groups = new Dictionary<int, List<SelectableObject>>();
        private static int lastGroup = -1, idleIndex;
        private static float lastTime;
        private static SelectionManager owner;

        public static void Handle(SelectionManager selection)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || selection == null) return;
            if (!ReferenceEquals(owner, selection))
            {
                groups.Clear(); owner = selection;
                lastGroup = -1; idleIndex = 0; lastTime = 0;
            }
            var ctrl = keyboard.ctrlKey.isPressed;
            for (var n = 1; n <= 9; n++)
            {
                if (!keyboard[Key.Digit1 + n - 1].wasPressedThisFrame) continue;
                if (ctrl)
                {
                    groups[n] = selection.GetSelectedObjects().Where(s => s != null).ToList();
                    ToastManager.Show(groups[n].Count > 0 ? $"부대 {n} 지정: {groups[n].Count}명" : $"부대 {n} 비움");
                }
                else Recall(selection, n);
                return;
            }
            if (keyboard.periodKey.wasPressedThisFrame) NextIdle(selection);
        }

        private static void Recall(SelectionManager selection, int n)
        {
            if (!groups.TryGetValue(n, out var list))
            { ToastManager.Show($"부대 {n}이(가) 비어 있습니다. Ctrl+{n}로 지정하세요."); return; }
            list.RemoveAll(s => s == null);
            var available = list.Where(s => s.isActiveAndEnabled).ToList();
            if (available.Count == 0)
            { ToastManager.Show($"부대 {n}에 현재 선택 가능한 대상이 없습니다."); return; }
            var twice = lastGroup == n && UnityEngine.Time.unscaledTime - lastTime < .4f;
            lastGroup = n; lastTime = UnityEngine.Time.unscaledTime;
            selection.SelectOnly(available[0]);
            foreach (var s in available.Skip(1)) selection.Add(s);
            if (twice) Focus(available[0].transform.position);
        }

        private static void NextIdle(SelectionManager selection)
        {
            var idle = CommanderRoster.Instance == null ? new List<CommanderAnt>() : CommanderRoster.Instance.Commanders
                .Where(c => c != null && c.isActiveAndEnabled && c.CanReceiveOrders && !c.IsAsleep && !c.IsEmbarked && !c.IsAwayFromHome && CommanderOverhead.Activity(c).Length == 0).ToList();
            if (idle.Count == 0) { ToastManager.Show("할 일 없는 장수가 없습니다."); return; }
            var c = idle[idleIndex++ % idle.Count];
            selection.SelectOnly(c.GetComponent<SelectableObject>());
            Focus(c.Position);
        }

        private static void Focus(UnityEngine.Vector3 point) => SceneObjects.Any<AntColony.Camera.IsometricCameraController>()?.FocusOn(point);
    }
}

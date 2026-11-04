using AntColony.Core;
using UnityEngine.InputSystem;

namespace AntColony.UI
{
    // 설정 화면의 단축키 변경. 버튼을 누른 뒤 다음에 누른 키로 바꾸고, Esc는 취소한다.
    public sealed partial class GameMenuController
    {
        private GameAction? rebinding;

        private bool PollRebind()
        {
            if (rebinding == null || Keyboard.current == null) return false;
            if (!open || ScreenName != "Settings") { rebinding = null; return false; }
            if (Keyboard.current.escapeKey.wasPressedThisFrame) { rebinding = null; Settings(); return true; }
            foreach (var control in Keyboard.current.allKeys)
            {
                if (control == null || !control.wasPressedThisFrame || !KeyBindings.CanBind(control.keyCode)) continue;
                var value = UserSettings.Current.Clone();
                KeyBindings.Bind(value, rebinding.Value, control.keyCode);
                UserSettings.Apply(value);
                rebinding = null;
                Settings();
                break;
            }
            return true;
        }
    }
}

using System;
using UnityEngine.InputSystem;

namespace AntColony.Core
{
    // 설정에서 바꿀 수 있는 단축키. 게임 속도 +/-, Esc, F1, F2, A(공격 이동)는 고정이다.
    public enum GameAction
    {
        RotateLeft, RotateRight, WeaponSkill, WingSkill, AddTroop, RemoveTroop, CycleWeapon,
        Roster, Pause, SciencePanel, EventLog, WorldMap, Diplomacy, Build
    }

    public static class KeyBindings
    {
        public static readonly Key[] Defaults =
            { Key.Z, Key.C, Key.Q, Key.W, Key.E, Key.D, Key.R, Key.G, Key.P, Key.K, Key.L, Key.M, Key.J, Key.B };
        // 이 키들은 고정 기능이 쓰므로 재지정할 수 없다.
        public static readonly Key[] Reserved =
            { Key.Escape, Key.F1, Key.F2, Key.A, Key.Equals, Key.Minus, Key.NumpadPlus, Key.NumpadMinus };

        public static Key Get(GameAction action) => Get(UserSettings.Current, action);
        public static Key Get(UserSettingsData settings, GameAction action)
        {
            var list = settings.keyBindings;
            var i = (int)action;
            return list != null && i < list.Count && Enum.TryParse<Key>(list[i], out var key) && CanBind(key) ? key : Defaults[i];
        }

        public static bool Pressed(GameAction action)
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard[Get(action)].wasPressedThisFrame;
        }

        // 다른 동작이 이미 쓰는 키면 두 동작의 키를 서로 바꾼다.
        public static bool CanBind(Key key) => Enum.IsDefined(typeof(Key), key) && key != Key.None && Array.IndexOf(Reserved, key) < 0;
        public static void Bind(UserSettingsData settings, GameAction action, Key key)
        {
            if (!CanBind(key)) return;
            var keys = new string[Defaults.Length];
            for (var i = 0; i < keys.Length; i++) keys[i] = Get(settings, (GameAction)i).ToString();
            var previous = keys[(int)action];
            for (var i = 0; i < keys.Length; i++) if (keys[i] == key.ToString()) keys[i] = previous;
            keys[(int)action] = key.ToString();
            settings.keyBindings = new System.Collections.Generic.List<string>(keys);
        }
    }
}

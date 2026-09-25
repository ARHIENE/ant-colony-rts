using System;
using System.IO;
using AntColony.Save;
using UnityEngine;

namespace AntColony.Core
{
    // 실제로 동작에 영향을 주는 값만 담는다. 아직 없는 기능(사운드 등)은 넣지 않는다.
    [Serializable]
    public class UserSettingsData
    {
        public int version = 1;
        public float cameraPanSpeed = 25f;       // IsometricCameraController.panSpeed
        public float edgeScrollThickness = 18f;  // 0이면 가장자리 스크롤을 끈다
        public bool autoSaveEnabled = true;
        public float autoSaveMinutes = 5f;       // 기본 5분, 1~30분
        public float toastSeconds = 6f;          // 알림 토스트 표시 시간
        public bool pauseSimulationOnMenu = true; // 명시적 일시정지에서 시뮬레이션도 멈출지
        public System.Collections.Generic.List<string> keyBindings = new System.Collections.Generic.List<string>(); // GameAction 순서, 비면 기본값

        public UserSettingsData Clone() => (UserSettingsData)MemberwiseClone();

        public void Sanitize()
        {
            cameraPanSpeed = Mathf.Clamp(cameraPanSpeed, 5f, 80f);
            edgeScrollThickness = Mathf.Clamp(edgeScrollThickness, 0f, 60f);
            autoSaveMinutes = Mathf.Clamp(autoSaveMinutes, 1f, 30f);
            toastSeconds = Mathf.Clamp(toastSeconds, 2f, 20f);
        }
    }

    // 설정 파일 하나를 읽고 쓰는 창구. 저장 슬롯과 같은 원자적 교체 규칙을 쓴다.
    public static class UserSettings
    {
        public const string FileName = "settings.json";

        private static UserSettingsData current;

        public static event Action OnChanged;

        public static UserSettingsData Current
        {
            get
            {
                if (current == null) Load();
                return current;
            }
        }

        public static string Path => System.IO.Path.Combine(SaveStorage.Root, FileName);

        public static void Load()
        {
            current = new UserSettingsData();
            try
            {
                if (File.Exists(Path))
                {
                    var loaded = JsonUtility.FromJson<UserSettingsData>(File.ReadAllText(Path));
                    if (loaded != null && loaded.version == new UserSettingsData().version) current = loaded;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Settings load failed, using defaults: " + e.Message);
            }
            current.Sanitize();
        }

        public static void Apply(UserSettingsData value, bool save = true)
        {
            if (value == null) return;
            current = value.Clone();
            current.Sanitize();
            if (save) Save();
            OnChanged?.Invoke();
        }

        public static void Save()
        {
            if (current == null) return;
            try { SaveStorage.WriteAtomic(Path, JsonUtility.ToJson(current, true)); }
            catch (Exception e) { Debug.LogWarning("Settings save failed: " + e.Message); }
        }

        // 검사 스크립트가 격리된 경로로 돌린 뒤 원래 값으로 되돌릴 때 쓴다.
        public static void ResetCacheForReload() => current = null;
    }
}

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
        public bool autoPauseThreat = true, autoPauseDowned = true, autoPauseRocket = true;
        public bool firstHints = true;           // 첫 등장 힌트 토스트
        public System.Collections.Generic.List<string> shownHints = new System.Collections.Generic.List<string>(); // 이미 본 힌트(1회)
        // 화면·소리(2026-10-25 설정창 재구성). 해상도 0이면 현재 모니터 해상도.
        public int screenMode = 0;               // 0 전체 화면(테두리 없음) 1 창 모드 2 전용 전체 화면
        public int resolutionWidth, resolutionHeight;
        public bool vSync = true;
        public int frameLimit = 60;              // 0 = 제한 없음
        public int quality = -1;                 // -1 = 프로젝트 기본
        public float masterVolume = 1f;          // 0~1

        public UserSettingsData Clone() => JsonUtility.FromJson<UserSettingsData>(JsonUtility.ToJson(this));

        public void Sanitize()
        {
            cameraPanSpeed = Mathf.Clamp(cameraPanSpeed, 5f, 80f);
            edgeScrollThickness = Mathf.Clamp(edgeScrollThickness, 0f, 60f);
            autoSaveMinutes = Mathf.Clamp(autoSaveMinutes, 1f, 30f);
            toastSeconds = Mathf.Clamp(toastSeconds, 2f, 20f);
            if (shownHints == null) shownHints = new System.Collections.Generic.List<string>();
            screenMode = Mathf.Clamp(screenMode, 0, 2); frameLimit = Mathf.Clamp(frameLimit, 0, 360);
            masterVolume = Mathf.Clamp01(masterVolume);
            if (quality >= QualitySettings.names.Length) quality = -1;
        }

        // 화면·소리 설정을 엔진에 반영한다. 에디터에서는 창 모드·해상도 변경을 건너뛴다(게임 뷰 크기 유지).
        private static int defaultQuality = -1; // 프로젝트 기본 품질(처음 적용 때 기억). '기본'을 고르면 이 값으로 되돌린다.
        public void ApplyRuntime()
        {
            if (defaultQuality < 0) defaultQuality = QualitySettings.GetQualityLevel();
            QualitySettings.vSyncCount = vSync ? 1 : 0;
            Application.targetFrameRate = vSync || frameLimit == 0 ? -1 : frameLimit;
            var level = quality >= 0 ? quality : defaultQuality;
            if (QualitySettings.GetQualityLevel() != level) QualitySettings.SetQualityLevel(level, true);
            AudioListener.volume = masterVolume;
#if !UNITY_EDITOR
            var mode = screenMode == 1 ? FullScreenMode.Windowed : screenMode == 2 ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.FullScreenWindow;
            int w = resolutionWidth > 0 ? resolutionWidth : Screen.currentResolution.width, h = resolutionHeight > 0 ? resolutionHeight : Screen.currentResolution.height;
            if (Screen.width != w || Screen.height != h || Screen.fullScreenMode != mode) Screen.SetResolution(w, h, mode);
#endif
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
            current.ApplyRuntime();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnStart() => Current.ApplyRuntime();

        public static void Apply(UserSettingsData value, bool save = true)
        {
            if (value == null) return;
            current = value.Clone();
            current.Sanitize();
            current.ApplyRuntime();
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

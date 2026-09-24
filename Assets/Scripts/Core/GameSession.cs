using UnityEngine;

namespace AntColony.Core
{
    // 씬을 다시 여는 동안 살아남는 유일한 게임 상태 보관소.
    // 새 게임 옵션과 "이 저장 파일을 불러와야 한다"는 요청만 들고 있고, 게임플레이 로직은 갖지 않는다.
    public sealed class GameSession : MonoBehaviour
    {
        private static GameSession instance;

        public static GameSession Instance
        {
            get
            {
                if (instance != null) return instance;
                var go = new GameObject("GameSession");
                instance = go.AddComponent<GameSession>();
                return instance;
            }
        }

        public static bool Exists => instance != null;

        [SerializeField] private NewGameOptions options = new NewGameOptions();

        // 아직 한 번도 새 게임/불러오기를 시작하지 않았으면 false다(첫 실행 = 메인 메뉴).
        // false인 동안에는 지형·NavMesh를 건드리지 않아 기존 씬 Play 동작이 그대로 유지된다.
        public bool GameStarted { get; private set; }

        // 다음 씬 로드 직후에 적용할 저장 파일. 적용되면 즉시 비워진다.
        public Save.SaveFileV1 PendingLoad { get; set; }

        // 이 세션에서 실제로 플레이한 시간(저장 파일 표시용). 메뉴에 머문 시간은 세지 않는다.
        public float PlaySeconds { get; private set; }
        public float GameSeconds { get; private set; }

        public NewGameOptions Options => options;

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!GameStarted || Save.SaveSystem.Busy || Time.timeScale <= 0) return;
            PlaySeconds += Time.unscaledDeltaTime;
            GameSeconds += Time.deltaTime;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        public void SetOptions(NewGameOptions value)
        {
            if (value == null) return;
            options = value.Clone();
        }

        public void MarkStarted(float playSeconds = 0f, float gameSeconds = 0f)
        {
            GameStarted = true;
            PlaySeconds = Mathf.Max(0f, playSeconds);
            GameSeconds = Mathf.Max(0f, gameSeconds);
        }

        public void MarkNotStarted()
        {
            GameStarted = false;
            PlaySeconds = 0f;
            GameSeconds = 0f;
        }
    }
}

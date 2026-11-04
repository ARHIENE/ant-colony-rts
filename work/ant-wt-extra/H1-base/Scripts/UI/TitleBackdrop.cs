using AntColony.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    // 시작 메뉴 전용 불투명 배경. 게임 장면이 비치지 않게 가리고, 지형도 등고선 무늬를 낮은 대비로 깐다.
    // 게임 화면이 열리면 스스로 사라진다. 저장을 불러오는 동안에는 유지한다(메인 메뉴로 돌아오면 앞 화면들이 다시 만든다).
    public sealed class TitleBackdrop : MonoBehaviour
    {
        private static Texture2D contours;

        public static void Ensure(Transform panel, bool loading = false)
        {
            if (!loading && GameSession.Instance != null && GameSession.Instance.GameStarted) return;
            var existing = panel.Find("TitleBackdrop");
            if (existing != null) { existing.SetAsFirstSibling(); return; }
            var rect = MenuTheme.Rect("TitleBackdrop", panel); MenuTheme.Stretch(rect);
            rect.SetAsFirstSibling();
            var image = rect.gameObject.AddComponent<RawImage>();
            image.texture = Contours(); image.raycastTarget = false;
            rect.gameObject.AddComponent<TitleBackdrop>();
        }

        private void Update()
        {
            var menu = GameMenuController.Instance;
            bool loading = menu != null && menu.ScreenName == "Loading...";
            if (GameSession.Instance != null && GameSession.Instance.GameStarted && !loading) Destroy(gameObject);
        }

        // 두 겹 펄린 노이즈의 등고선. 5번째 선마다 조금 밝게(지형도의 계곡선) 그린다.
        private static Texture2D Contours()
        {
            if (contours != null) return contours;
            const int w = 960, h = 540;
            contours = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            Color ground = MenuTheme.Hex(0x1f201e), line = MenuTheme.Hex(0x2b2c29), index = MenuTheme.Hex(0x363732);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float nx = x / 260f, ny = y / 260f;
                    float v = Mathf.PerlinNoise(nx + 11.3f, ny + 4.7f) * .75f + Mathf.PerlinNoise(nx * 2.3f + 3.1f, ny * 2.3f + 8.2f) * .25f;
                    float level = v * 18f, fraction = level - Mathf.Floor(level);
                    bool onLine = fraction < .07f;
                    pixels[y * w + x] = !onLine ? ground : ((int)Mathf.Floor(level)) % 5 == 0 ? index : line;
                }
            contours.SetPixels(pixels); contours.Apply(false, true);
            return contours;
        }
    }
}

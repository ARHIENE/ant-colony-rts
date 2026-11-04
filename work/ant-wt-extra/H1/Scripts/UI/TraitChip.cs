using System.Collections.Generic;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    // 특성 한 칸(팰월드 PASSIVE SKILL 칸 참고, 2026-10-25 사용자 요청): 어두운 바탕 + 등급색 테두리·오른쪽 그라데이션 + 오른쪽 화살표.
    // 실버 ▲1(무늬 없음), 골드 ▲3, 다이아 ▲3+각면 무늬, 레드 ▼3+각면 무늬(붉은 다이아). 텍스처는 등급·크기별로 한 번만 만든다.
    public static class TraitChip
    {
        private static readonly Dictionary<(TraitGrade, int, int), Sprite> cache = new Dictionary<(TraitGrade, int, int), Sprite>();

        public static Color GradeColor(TraitGrade g) => g switch {
            TraitGrade.Gold => MenuTheme.Hex(0xe8b830), TraitGrade.Diamond => MenuTheme.Hex(0x3fd6d0),
            TraitGrade.Red => MenuTheme.Hex(0xe0383c), _ => MenuTheme.Hex(0xc9ccd2) };

        public static RectTransform Chip(Transform parent, CommanderTrait t, float x, float y, float w, float h)
        {
            var grade = TraitRules.Grade(t);
            var rect = MenuLayout.Place(MenuTheme.Rect("Trait " + t, parent), x, y, w, h);
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = Sprite(grade, (int)w, (int)h); image.raycastTarget = false;
            var label = MenuLayout.Label(rect, CommanderTraits.DisplayName(t), 12, 8, 0, w - 8 - h * 1.4f, h, Color.white);
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate; // 좁은 칸에선 글자를 줄인다
            label.resizeTextForBestFit = true; label.resizeTextMinSize = 9; label.resizeTextMaxSize = 12;
            return rect;
        }

        // 특성 목록을 columns열 칸으로 배치한다. 없으면 '특성 없음'.
        public static void Grid(Transform parent, IReadOnlyList<CommanderTrait> traits, float x, float y, float w, float rowH, int columns, float gap = 4)
        {
            if (traits.Count == 0) { MenuLayout.Label(parent, "특성 없음", 12, x, y, w, rowH, MenuTheme.Muted); return; }
            var cw = (w - gap * (columns - 1)) / columns;
            for (var i = 0; i < traits.Count; i++)
                Chip(parent, traits[i], x + i % columns * (cw + gap), y + i / columns * (rowH + gap), cw, rowH);
        }

        private static Sprite Sprite(TraitGrade g, int w, int h)
        {
            if (cache.TryGetValue((g, w, h), out var s) && s != null) return s;
            const int k = 2; // 고해상도 화면용 2배
            int W = Mathf.Max(8, w * k), H = Mathf.Max(8, h * k);
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[W * H];
            var baseC = new Color(.11f, .13f, .16f, .94f); var gc = GradeColor(g);
            var facets = g == TraitGrade.Diamond || g == TraitGrade.Red;
            var cell = H * 3 / 4; // 각면 무늬 한 칸
            for (var yy = 0; yy < H; yy++)
                for (var xx = 0; xx < W; xx++)
                {
                    var c = baseC;
                    var u = (float)xx / W;
                    if (g != TraitGrade.Silver && u > .4f)
                    {
                        var t = Mathf.Pow((u - .4f) / .6f, 1.2f) * .85f;
                        if (facets)
                        {
                            // 정사각 칸을 대각선으로 나눈 삼각형마다 밝기를 달리한다.
                            int cx = xx / cell, cy = yy / cell; var upper = (cx + cy) % 2 == 0 ? xx % cell > yy % cell : cell - xx % cell > yy % cell; // 대각선 방향을 번갈아 조각 느낌
                            var hsh = Mathf.Abs(Mathf.Sin((cx * 12.99f + cy * 78.23f + (upper ? 4.1f : 0)) * 43.75f)) % 1f;
                            t *= .4f + 1.0f * hsh;
                        }
                        c = Color.Lerp(baseC, gc, Mathf.Clamp01(t));
                    }
                    if (xx < 2 * k) c = Color.Lerp(c, gc, .9f); // 왼쪽 강조선
                    else if (xx == W - 1 || yy == 0 || yy == H - 1) c = Color.Lerp(c, gc, .6f); // 테두리
                    px[yy * W + xx] = c;
                }
            // 오른쪽 화살표(실버 1개, 나머지 3개, 레드는 아래 방향).
            var n = g == TraitGrade.Silver ? 1 : 3; var down = g == TraitGrade.Red;
            var ink = g == TraitGrade.Silver ? Color.white : Color.Lerp(gc, Color.white, .65f);
            float cxp = W - H * .8f, half = H * .32f, step = H * .2f, thick = 1.1f * k;
            for (var i = 0; i < n; i++)
            {
                var apexY = H * .5f + (i - (n - 1) * .5f) * step + (down ? -half * .5f : half * .5f);
                var armY = apexY + (down ? half : -half);
                var a = new Vector2(cxp, apexY);
                var l = new Vector2(cxp - half * 1.3f, armY); var r = new Vector2(cxp + half * 1.3f, armY);
                for (var yy = 0; yy < H; yy++)
                    for (var xx = (int)(cxp - half * 1.5f); xx <= (int)(cxp + half * 1.5f); xx++)
                    {
                        if (xx < 0 || xx >= W) continue;
                        var p = new Vector2(xx + .5f, yy + .5f);
                        var d = Mathf.Min(Dist(p, a, l), Dist(p, a, r));
                        var cov = Mathf.Clamp01(thick - d + .5f);
                        if (cov > 0) px[yy * W + xx] = Color.Lerp(px[yy * W + xx], ink, cov);
                    }
            }
            tex.SetPixels(px); tex.Apply();
            s = UnityEngine.Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(.5f, .5f), 100f * k);
            cache[(g, w, h)] = s; return s;
        }

        private static float Dist(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a; var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }
    }
}

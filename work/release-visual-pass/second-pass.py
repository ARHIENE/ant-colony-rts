from pathlib import Path
p=Path('Assets/_Project/Scripts/UI/BuildScreen.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('for (var i = 0; i < Tabs[t].Length; i++)\n                {','var content = CreateBuildScroll(page);\n                for (var i = 0; i < Tabs[t].Length; i++)\n                {',1).replace('var button = Cell(page, ButtonName(entry)', 'var button = Cell(content, ButtonName(entry)')
s=s.replace('var cols = buttons.Length > 12 ? 4 : 3;\n                    var rowHeight = Mathf.Min(96f, ((RectTransform)page.transform).rect.height / Mathf.Max(1, (buttons.Length + cols - 1) / cols));','''const int cols = 3;
                    float rowHeight = Mathf.Max(92f, 66f / pixels);
                    var scroll = page.GetComponent<ScrollRect>();
                    float gridWidth = width - 18;
                    scroll.content.sizeDelta = new Vector2(0, Mathf.CeilToInt(buttons.Length / (float)cols) * rowHeight);
                    scroll.verticalScrollbar.gameObject.SetActive(scroll.content.sizeDelta.y > scroll.viewport.rect.height);''')
s=s.replace('i % cols * width / cols, i / cols * rowHeight, width / cols - 4','i % cols * gridWidth / cols, i / cols * rowHeight, gridWidth / cols - 6')
s=s.replace('Mathf.Max(16, Mathf.CeilToInt(11 / pixels))','Mathf.Max(17, Mathf.CeilToInt(13 / pixels))')
s=s.replace('MenuLayout.Label(buildWindow, "건물 선택 → 재료 비교 → 담당 장수 선택 → 배치", 12,','MenuLayout.Label(buildWindow, "건물 선택 → 담당 장수 → 배치   ·   휠로 목록 이동", 16,')
idx=s.index('        private static Button Cell(')
s=s[:idx]+'''        private static RectTransform CreateBuildScroll(RectTransform page)
        {
            var scroll = page.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 45; scroll.inertia = false;
            var view = MenuTheme.Rect("Viewport", page); MenuTheme.Stretch(view);
            view.offsetMax = new Vector2(-18, 0);
            view.gameObject.AddComponent<Image>().color = Color.white;
            view.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var content = MenuTheme.Rect("Content", view);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
            scroll.content = content; scroll.viewport = view;
            var track = MenuTheme.Rect("Scrollbar", page); MenuTheme.Stretch(track);
            track.anchorMin = new Vector2(1, 0); track.offsetMin = new Vector2(-10, 0); track.offsetMax = Vector2.zero;
            track.gameObject.AddComponent<Image>().color = MenuTheme.Well;
            var handle = MenuTheme.Rect("Handle", track); MenuTheme.Stretch(handle);
            var thumb = handle.gameObject.AddComponent<Image>(); thumb.color = MenuTheme.Muted;
            var bar = track.gameObject.AddComponent<Scrollbar>(); bar.handleRect = handle;
            bar.targetGraphic = thumb; bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar;
            scroll.onValueChanged.AddListener(_ => GameMenuController.Instance?.Tooltip(""));
            return content;
        }

'''+s[idx:];p.write_text(s,encoding='utf-8')
p=Path('Assets/_Project/Scripts/Map/MapGenerator.cs');s=p.read_text(encoding='utf-8-sig');needle='            var layersCount = Mathf.Min(terrainLayers.Count, 32);\n            ReleaseObject(terrainTextures);';assert needle in s
s=s.replace(needle,'''            // Imported ground paintings cover several metres, not a one-metre checkerboard.
            // Older materials still carry the shader default of 1; keep authored larger scales.
            if (terrainMaterial.GetFloat("_textureScale") <= 1.01f)
                terrainMaterial.SetFloat("_textureScale", 8f);
'''+needle);p.write_text(s,encoding='utf-8')

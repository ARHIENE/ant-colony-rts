from pathlib import Path
p=Path('work/release-visual-pass'); b=p/'before'; d=p/'draft'
s=(b/'MapGenerator.cs').read_text(encoding='utf-8-sig')
s=s.replace('private bool styled;', 'private bool styled;\n        private string dressingBiome = "Forest";')
s=s.replace('if (style == null) return;','if (style == null) return;\n            dressingBiome = style.name;',1)
s=s.replace('try { SpawnObjects(); }','try { SpawnObjects(); if (!spawnObjects.Any(s => s.prefab != null)) SpawnMissingDressing(); }')
mark='        private Material FoliageClone('
idx=s.index(mark)
method='''        // Restore visual dressing only when the old biome prefab references are missing.
        // No resource stock, navigation obstacles or random stream changes; the settlement core
        // and four approach corridors remain clear. Existing authored spawns take precedence.
        private void SpawnMissingDressing()
        {
            if (!Application.isPlaying || !hasFlatZone || mesh == null) return;
            var vertices = mesh.vertices;
            var rng = new System.Random(spawnSeedBase ^ 0x4c31);
            bool wooded = dressingBiome == "Forest" || dressingBiome == "Garden" || dressingBiome == "Waterside";
            int count = 0;
            for (int z = 2; z < zSize - 2 && count < 180; z += 4)
                for (int x = 2; x < xSize - 2 && count < 180; x += 4)
                {
                    int i = z * (xSize + 1) + x;
                    if (terrace.clear[i] || !terrace.open[i]) continue;
                    var floor = transform.TransformPoint(vertices[i]);
                    var delta = floor - flatCenter;
                    float distance = new Vector2(delta.x, delta.z).magnitude;
                    if (distance < flatRadius * .86f || distance > flatRadius + 28f) continue;
                    if (Mathf.Abs(delta.x) < 4f || Mathf.Abs(delta.z) < 4f) continue;
                    // Broad clumps, not an even checkerboard of props.
                    float patch = Mathf.PerlinNoise((x + xOffset) * .075f, (z + zOffset) * .075f);
                    if (patch < .46f || rng.NextDouble() > .63) continue;
                    bool tree = wooded && distance > flatRadius + 5f && rng.NextDouble() < .34;
                    string id = tree ? (rng.NextDouble() < .7 ? "Oak" : "Pine") : wooded && rng.NextDouble() < .68 ? "Bush" : "Rock";
                    float width = (tree ? 4.2f : id == "Bush" ? 1.1f : .7f) * (.8f + (float)rng.NextDouble() * .5f);
                    var model = AntColony.World.ProductionProp.Attach(transform, $"Environment1110/Models/{id}/{id}", width, floor);
                    if (model == null) continue;
                    model.name = "Landscape " + id;
                    model.transform.RotateAround(floor, Vector3.up, (float)rng.NextDouble() * 360f);
                    // Decorative objects never consume clicks or enter the navigation bake.
                    model.layer = 2;
                    foreach (var child in model.GetComponentsInChildren<Transform>()) child.gameObject.layer = 2;
                    foreach (var collider in model.GetComponentsInChildren<Collider>()) collider.enabled = false;
                    spawnedObjects.Add(model); count++;
                }
        }

'''
s=s[:idx]+method+s[idx:];(d/'MapGenerator.cs').write_text(s,encoding='utf-8')
s=(b/'CommanderOverhead.cs').read_text(encoding='utf-8-sig')
s=s.replace('if (!bodies.TryGetValue(c, out var renderers)) bodies[c] = renderers = c.GetComponentsInChildren<SkinnedMeshRenderer>();','if (!bodies.TryGetValue(c, out var renderers) || Time.frameCount % 120 == 0)\n                bodies[c] = renderers = c.GetComponentsInChildren<SkinnedMeshRenderer>();')
s=s.replace('!renderer.enabled ||','!renderer.enabled || renderer.forceRenderingOff ||')
s=s.replace('if (!selected && !hurt && !MoodAlert(c) && !c.IsDeployed) continue;','// A healthy, unselected commander still needs a readable identity in the world.')
s=s.replace('float scale = MenuTheme.UiScale, line = 16 * scale;', 'float scale = Mathf.Max(.85f, MenuTheme.UiScale), line = 20 * scale;')
s=s.replace('Mathf.Max(8, Mathf.RoundToInt(12 * scale))','Mathf.Max(12, Mathf.RoundToInt(13 * scale))')
s=s.replace('float height = line + (hurt || c.IsDeployed ? 9 * scale : 0);','string activity = Activity(c);\n                if (activity.Length == 0) activity = "대기";\n                float detail = selected ? 17 * scale : 0;\n                float height = line + detail + (hurt || c.IsDeployed ? 9 * scale : 0);')
s=s.replace('new Rect(x, y, width, line), Texture2D.whiteTexture','new Rect(x, y, width, line + detail), Texture2D.whiteTexture')
s=s.replace('GUI.Label(new Rect(x, y, width, line), c.CommanderName, nameStyle);','''GUI.Label(new Rect(x, y, width, line), c.CommanderName, nameStyle);
                GUI.color = selected ? MenuTheme.Accent : MenuTheme.Muted;
                GUI.DrawTexture(new Rect(x, y + line + detail - 2 * scale, width, 2 * scale), Texture2D.whiteTexture);
                GUI.color = old;
                if (selected)
                {
                    tagStyle.font = MenuTheme.Font; tagStyle.fontSize = Mathf.RoundToInt(11 * scale);
                    tagStyle.normal.background = null; tagStyle.normal.textColor = MenuTheme.Muted;
                    GUI.Label(new Rect(x, y + line - 2 * scale, width, detail), activity, tagStyle);
                }''')
s=s.replace('y + line + scale','y + line + detail + scale').replace('y + line + 5 * scale','y + line + detail + 5 * scale')
(d/'CommanderOverhead.cs').write_text(s,encoding='utf-8')
s=(b/'MenuViewport.cs').read_text(encoding='utf-8-sig').replace('public bool FitSelf;','public bool FitSelf;\n        public float MaxScale = 1f;')
s=s.replace('Mathf.Min(1, (viewport.x','Mathf.Min(MaxScale, (viewport.x');(d/'MenuViewport.cs').write_text(s,encoding='utf-8')
s=(b/'BuildScreen.cs').read_text(encoding='utf-8-sig').replace('buildWindow.gameObject.AddComponent<MenuViewport>().FitSelf = true;','var viewport = buildWindow.gameObject.AddComponent<MenuViewport>();\n            viewport.FitSelf = true; viewport.MaxScale = 1.24f;')
s=s.replace('layoutSize != buildPanel.rect.size','layoutSize != buildPanel.rect.size || layoutScreenWidth != Screen.width')
s=s.replace('layoutSize = buildPanel.rect.size;', 'layoutSize = buildPanel.rect.size; layoutScreenWidth = Screen.width;\n                float pixels = Mathf.Max(.1f, buildWindow.lossyScale.x);')
s=s.replace('private void Update()','private int layoutScreenWidth;\n        private void Update()')
s=s.replace('MenuTheme.Stretch(tabButtons[i].GetComponentsInChildren<Text>(true)[1].rectTransform);','var tabLabel = tabButtons[i].GetComponentsInChildren<Text>(true)[1];\n                    tabLabel.fontSize = Mathf.Max(14, Mathf.CeilToInt(11 / pixels));\n                    MenuTheme.Stretch(tabLabel.rectTransform);')
s=s.replace('label.verticalOverflow = VerticalWrapMode.Truncate; label.resizeTextForBestFit = true; label.resizeTextMinSize = 11; label.resizeTextMaxSize = 16;', 'label.alignment = TextAnchor.MiddleCenter;\n                        label.verticalOverflow = VerticalWrapMode.Truncate; label.resizeTextForBestFit = false;\n                        label.fontSize = Mathf.Max(16, Mathf.CeilToInt(11 / pixels));')
s=s.replace('label.text = entries[i].name +','label.text = "<b>" + entries[i].name + "</b>" +')
(d/'BuildScreen.cs').write_text(s,encoding='utf-8')
print('4 drafts prepared')

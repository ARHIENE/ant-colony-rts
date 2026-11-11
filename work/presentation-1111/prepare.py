from pathlib import Path
p=Path('work/presentation-1111/draft')
def edit(name, old, new):
    f=p/name;s=f.read_text(encoding='utf-8-sig');assert old in s,name;s=s.replace(old,new);f.write_text(s,encoding='utf-8')
edit('BuildScreen.cs','        public static bool Picking => IsOpen && instance.picking != null;', '''        public static bool Picking => IsOpen && instance.picking != null;

        // Building help belongs in the free right column, never over the building grid.
        public static bool TooltipArea(RectTransform source, out RectTransform area)
        {
            area = null;
            if (!IsOpen || source == null || !source.IsChildOf(instance.buildPanel)) return false;
            if (!Picking) area = instance.picker;
            return true;
        }''')
edit('BuildScreen.cs','            picking = null;\n            buildWindow','            GameMenuController.Instance?.Tooltip("");\n            picking = null;\n            buildWindow')
edit('BuildScreen.cs','            picking = entry;','            GameMenuController.Instance?.Tooltip("");\n            picking = entry;')
edit('GameMenuController.cs','            tip.text = value;','''            if (BuildScreen.TooltipArea(source, out var descriptionArea))
            {
                if (descriptionArea == null || string.IsNullOrEmpty(value))
                { tooltipPanel.gameObject.SetActive(false); return; }
                var parent = (RectTransform)tooltipPanel.parent;
                var corners = new Vector3[4]; descriptionArea.GetWorldCorners(corners);
                var bottom = parent.InverseTransformPoint(corners[0]);
                var top = parent.InverseTransformPoint(corners[2]);
                tip.text = value; tip.fontSize = 14; tip.alignment = TextAnchor.UpperLeft;
                tooltipPanel.anchorMin = tooltipPanel.anchorMax = tooltipPanel.pivot = Vector2.zero;
                tooltipPanel.sizeDelta = new Vector2(top.x - bottom.x, top.y - bottom.y);
                tooltipPanel.anchoredPosition = new Vector2(bottom.x - parent.rect.xMin, bottom.y - parent.rect.yMin);
                tooltipPanel.gameObject.SetActive(true); tooltipPanel.SetAsLastSibling();
                return;
            }
            tip.text = value;''')
edit('MapGenerator.cs','            var layersCount = Mathf.Min(terrainLayers.Count, 32);\n            ReleaseObject(terrainTextures);','''            // Old saves and direct scene Play may have no biome style. Repair only the
            // visual layers; do not apply a biome's water, terrain, spawns or resource rules.
            if (!terrainLayers.Exists(layer => layer.texture != null))
            {
                var fallback = BiomeMapStyle.For(AntColony.Core.MapBiome.Forest);
                if (fallback != null) terrainLayers = fallback.layers.FindAll(layer => layer.texture != null);
            }
            var layersCount = Mathf.Min(terrainLayers.Count, 32);
            ReleaseObject(terrainTextures);''')
edit('GoalChain.cs','        public static string RecentAchievement', '        public static string HudText => Describe(CurrentStage);\n        public static string RecentAchievement')
edit('BetaProgress.cs','new Vector2(360, 112)', 'new Vector2(360, 84)')
edit('BetaProgress.cs','objective = MenuTheme.Text(panel, "", 13, 80);','objective = MenuTheme.Text(panel, "", 14, 80);\n            HudChrome.Round(panel.GetComponent<UnityEngine.UI.Image>(), 5, MenuTheme.Line);')
edit('BetaProgress.cs','            objective.text = emphasized ?','            var brief = GoalChain.HudText;\n            int rewardLine = brief.IndexOf("\\n<color=#d6d7cb>이후");\n            if (rewardLine >= 0) brief = brief.Substring(0, rewardLine);\n            objective.text = emphasized ?')
edit('BetaProgress.cs','+ CurrentObjective : CurrentObjective;', '+ brief : brief;')
edit('BetaProgress.cs','Mathf.Max(112, objective.preferredHeight + 16)', 'Mathf.Max(76, objective.preferredHeight + 20)')
edit('CharacterPortraits.cs','            tex = Render(source.gameObject);','            tex = Render(source.gameObject);')
edit('CharacterPortraits.cs','            foreach (var t in clone.GetComponentsInChildren<Transform>(true))','''            // Native world animation suppresses the legacy body with forceRenderingOff.
            // A portrait is an isolated snapshot: restore this only on its clone.
            foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true)) renderer.forceRenderingOff = false;
            foreach (var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (var animator in clone.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (var t in clone.GetComponentsInChildren<Transform>(true))''')
edit('CharacterPortraits.cs','if (t.name.StartsWith("Socket."))','if (t.name.StartsWith("Socket.") || t.name == "Native equipment grip")')

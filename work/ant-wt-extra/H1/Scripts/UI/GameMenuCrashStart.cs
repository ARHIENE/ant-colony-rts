using System;
using System.Linq;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;
using UnityEngine.UI;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        private void DrawCrashStart(RectTransform f)
        {
            if (options.startingTeam == null || options.startingTeam.Length != 3)
                options.startingTeam = Enumerable.Range(0, 3).Select(StartingCandidate.Roll).ToArray();
            var p = L.Plate(f, "NewGamePanel", 120, 90, 1200, 720);
            L.Label(p, "불시착 생존자", 20, 24, 10, 400, 38, bold: true);
            L.Label(p, "시민과 건물 없이 정착을 시작합니다", 12, 440, 10, 720, 38, MenuTheme.Muted);
            L.Line(p, 0, 56, 1200);
            var settings = L.List(p, 24, 72, 290, 546);
            var settingsScroll = settings.GetComponentInParent<ScrollRect>();
            settingsScroll.vertical = false; settingsScroll.inertia = false; settingsScroll.movementType = ScrollRect.MovementType.Clamped;
            // newMap: 맵 모양이 바뀌는 선택(크기·기후·지역)은 시드도 새로 뽑는다(사용자 요청 2026-10-25).
            void Choices<T>(string title, T[] values, Func<T, string> label, T current, Action<T> choose, bool newMap = false)
            {
                var row = L.Cell(settings, title, 70);
                L.Label(row, title, 15, 0, 0, 260, 26, bold: true);
                var width = 260f / values.Length;
                for (var i = 0; i < values.Length; i++)
                {
                    var item = values[i]; var selected = Equals(current, item);
                    var b = L.Button(row, title + " " + item, label(item), i * width, 28, width - 6, 34,
                        () => { ReadSeed(); choose(item); if (newMap && !Equals(current, item)) options.seed = NewGameOptions.RandomSeed(); NewGameScreen(); });
                    if (selected) MarkSelected(b);
                }
            }
            Choices("맵 크기", (MapSize[])Enum.GetValues(typeof(MapSize)), s => new[] { "소", "중", "대" }[(int)s], options.mapSize, s => options.mapSize = s, true);
            Choices("침공 난이도", (DifficultyLevel[])Enum.GetValues(typeof(DifficultyLevel)), s => new[] { "온화", "보통", "가혹" }[(int)s], options.difficulty, s => options.difficulty = s);
            Choices("기후", (MapClimate[])Enum.GetValues(typeof(MapClimate)), s => new[] { "온대", "한랭", "열대습윤" }[(int)s], options.climate, s => options.climate = s, true);
            Choices("장수 사망", (CommanderDeathMode[])Enum.GetValues(typeof(CommanderDeathMode)), s => new[] { "관대", "보통", "가혹" }[(int)s], options.commanderDeath, s => options.commanderDeath = s);
            var biome = L.Cell(settings, "Starting Climate", 130);
            L.Label(biome, "시작 지역", 15, 0, 0, 260, 26, bold: true);
            var allBiomes = new[] { MapBiome.None }.Concat(BiomeRules.All).ToArray();
            for (var i = 0; i < allBiomes.Length; i++)
            {
                var b = allBiomes[i];
                var button = L.Button(biome, "Biome " + b, b == MapBiome.None ? "무작위" : (b == MapBiome.City ? "숲 폐허" : b == MapBiome.Cave ? "고산·동굴" : BiomeRules.Name(b).Replace(" 지대", "")),
                    i % 3 * 88, 30 + i / 3 * 32, 82, 28, () => { ReadSeed(); if (options.biome != b) options.seed = NewGameOptions.RandomSeed(); options.biome = b; NewGameScreen(); });
                if (options.biome == b) MarkSelected(button);
            }
            var seedRow = L.Cell(settings, "Seed", 72);
            L.Label(seedRow, "맵 시드", 15, 0, 0, 260, 26, bold: true);
            seed = MenuTheme.Input(seedRow, options.seed.ToString()); L.Place((RectTransform)seed.transform, 0, 30, 178, 34); seed.textComponent.fontSize = 13;
            L.Button(seedRow, "Random Seed", "무작위", 184, 30, 78, 34, () => { options.seed = NewGameOptions.RandomSeed(); seed.text = options.seed.ToString(); });

            L.Label(p, "시작 인원", 15, 338, 72, 100, 32, bold: true);
            for (var i = 1; i <= 3; i++)
            {
                var n = i; var b = L.Button(p, "Starting Count " + n, n + "명", 440 + (i - 1) * 58, 72, 50, 32,
                    () => { ReadSeed(); options.startingCount = n; NewGameScreen(); });
                if (n == options.startingCount) MarkSelected(b);
            }
            L.Label(p, "슬롯별 전체 재추첨 · 횟수 제한 없음", 12, 650, 72, 520, 32, MenuTheme.Muted, TextAnchor.MiddleRight);
            for (var i = 0; i < options.startingCount; i++) DrawStartingCandidate(p, i, 338 + i * 280, 118);
            var selectedTeam = options.startingTeam.Take(options.startingCount).ToArray();
            var missing = StartingCandidate.Missing(selectedTeam);
            L.Label(p, missing.Length == 0 ? "필수 작업 7종 수행 가능" : "수행자 부족: " + string.Join(" · ", missing), 13,
                338, 598, 834, 32, missing.Length == 0 ? MenuTheme.Muted : MenuTheme.Warning);
            L.Line(p, 0, 642, 1200);
            L.Button(p, "Back", "뒤로", 24, 660, 100, 38, Main);
            L.Label(p, "1인 14끼 · 나무칼 · 나무 도구 · 지역에 맞는 옷\n잔해 회수량은 인원과 무관합니다", 12, 144, 652, 680, 56, MenuTheme.Muted);
            var start = L.Button(p, "Start Game", "게임 시작", 974, 660, 200, 38, () =>
            {
                if (!ReadSeed()) return;
                var chosen = options.Clone(); chosen.startingTeam = selectedTeam;
                if (chosen.biome == MapBiome.None) chosen.biome = BiomeRules.Random();
                try { CrashLandingStart.Begin(chosen); } catch (Exception e) { ToastManager.Show(e.Message); }
            }, missing.Length == 0 ? "선택한 생존자로 시작합니다" : "부족한 작업 수행자를 재추첨하세요", true);
            start.interactable = missing.Length == 0;
        }

        // 기술 수치 색(FM식): 0~2 흐림, 3~5 보통, 6~9 연두, 10~14 초록, 15 이상 금색.
        internal static Color SkillColor(int level) => level >= 15 ? MenuTheme.Hex(0xf2c14e) : level >= 10 ? MenuTheme.Hex(0x6fd36f)
            : level >= 6 ? MenuTheme.Hex(0xb6e07a) : level >= 3 ? MenuTheme.Hex(0xe8e6da) : MenuTheme.Hex(0x8a8b82);
        // 선택된 버튼: 테두리만 바꾸던 것을 채운 배경·굵은 글씨·두꺼운 테두리로(선택 여부가 안 보이던 문제).
        internal static void MarkSelected(Button b)
        {
            var colors = b.colors; colors.normalColor = Color.Lerp(MenuTheme.Plate2, MenuTheme.Selection, .5f);
            colors.highlightedColor = colors.selectedColor = Color.Lerp(MenuTheme.Plate2, MenuTheme.Selection, .65f); MenuTheme.SetColorsInstant(b, colors);
            var outline = b.GetComponent<Outline>(); outline.effectColor = MenuTheme.Selection; outline.effectDistance = new Vector2(2, -2);
            var text = b.GetComponentInChildren<Text>(); text.color = Color.white; text.fontStyle = FontStyle.Bold;
        }

        private void DrawStartingCandidate(RectTransform p, int slot, float x, float y)
        {
            var c = options.startingTeam[slot];
            var card = L.Box(p, "Starting Candidate " + slot, x, y, 264, 466, MenuTheme.Plate2);
            L.Label(card, c.name + " · " + c.ageYears + "세", 15, 12, 8, 240, 30, bold: true);
            L.Button(card, "Reroll Candidate " + slot, "전체 재추첨", 12, 42, 240, 32, () =>
            { ReadSeed(); options.startingTeam[slot] = StartingCandidate.Roll(slot); NewGameScreen(); }, "능력·특성·나이·외형을 모두 새로 뽑습니다");
            L.Label(card, "외형: " + StartingCandidate.AppearanceName(c.appearanceSeed), 12, 12, 80, 240, 22, MenuTheme.Muted);
            L.Label(card, "기후 적합: " + (options.biome == MapBiome.None && options.climate == MapClimate.Temperate ? "지역 결정 후 복장 지급" : StartingCandidate.Clothes(options.biome, options.climate)), 12, 12, 104, 240, 22, MenuTheme.Muted);
            TraitChip.Grid(card, c.traits.values, 12, 130, 240, 22, 2);
            L.Label(card, "작업 불가: " + (c.Blocked.Length == 0 ? "없음" : c.Blocked), 12, 12, 180, 240, 42,
                c.Blocked.Length == 0 ? MenuTheme.Muted : MenuTheme.Warning);
            L.Line(card, 12, 226, 240);
            L.Label(card, "현재 기술 / 열정", 13, 12, 232, 240, 24, bold: true);
            for (var i = 0; i < CommanderTalents.Count; i++)
            {
                var skill = (CommanderActivity)i; var flame = c.traits.Flame(skill);
                // FM처럼 수치를 색으로 구별하고, 열정은 숫자에 붙이지 않고 주황 ▲ 표시로 따로 둔다.
                var level = c.talents.Level(skill);
                L.Label(card, $"{StartingCandidate.SkillName(skill)} <color=#{ColorUtility.ToHtmlStringRGB(SkillColor(level))}><b>{level}</b></color>"
                    + (flame > 0 ? " <color=#f2a93b>" + new string('▲', flame) + "</color>" : ""), 12,
                    12 + i / 8 * 122, 262 + i % 8 * 22, 120, 22);
            }
            L.Line(card, 12, 440, 240);
            L.Label(card, "잠재력 미평가 · 기존 관계 없음", 12, 12, 442, 240, 22, MenuTheme.Muted);
        }
    }
}

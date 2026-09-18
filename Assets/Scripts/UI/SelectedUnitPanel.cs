using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    public class SelectedUnitPanel : MonoBehaviour
    {
        private SelectionManager selection;
        private GameObject panel;
        private Text title;
        private Text healthText;
        private Text combatStatsText;
        private Text workText;
        private RectTransform healthFill;
        private CommanderAnt selectedCommander;
        private Text powerStrikeText;
        private Text stanceText;

        // 현재 선택에서 유효한 유닛이 정확히 한 명이고 장수이면 반환한다. 행동 시점에 캐시 없이 조회한다.
        public static CommanderAnt FindSingleSelectedCommander(SelectionManager selection)
        {
            if (selection == null) return null;
            AntUnitBase found = null;
            foreach (var selectable in selection.GetSelectedObjects())
            {
                if (selectable == null || !selectable.isActiveAndEnabled || !selectable.IsSelected) continue;
                var unit = selectable.GetComponent<AntUnitBase>();
                if (unit == null || !unit.isActiveAndEnabled || unit.IsDead || unit.Data == null) continue;
                if (found != null) return null;
                found = unit;
            }
            return found as CommanderAnt;
        }

        private void Start()
        {
            selection = FindFirstObjectByType<SelectionManager>();
            var background = CreateImage("SelectedUnitPanel", transform, new Color(0.08f, 0.12f, 0.09f, 0.95f));
            panel = background.gameObject;
            var rect = background.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(10f, 105f);
            rect.sizeDelta = new Vector2(460f, 185f);
            title = CreateText("UnitName", rect, new Vector2(12f, -10f));
            healthText = CreateText("Health", rect, new Vector2(12f, -38f));
            combatStatsText = CreateText("CombatStats", rect, new Vector2(12f, -66f));
            // 패널을 25px 키워 버튼 줄(위쪽 끝 y=56) 위에 채집 숙련도 전용 줄을 둔다.
            workText = CreateText("WorkProficiency", rect, new Vector2(12f, -94f));

            var track = CreateImage("HealthTrack", rect, new Color(0.2f, 0.25f, 0.22f));
            track.rectTransform.anchorMin = track.rectTransform.anchorMax = track.rectTransform.pivot = Vector2.zero;
            track.rectTransform.anchoredPosition = new Vector2(12f, 12f);
            track.rectTransform.sizeDelta = new Vector2(276f, 12f);
            healthFill = CreateImage("HealthFill", track.transform, new Color(0.3f, 0.85f, 0.4f)).rectTransform;
            healthFill.anchorMin = Vector2.zero;
            healthFill.anchorMax = Vector2.one;
            healthFill.offsetMin = healthFill.offsetMax = Vector2.zero;
            CreateButton(rect, 12f, "+1 Ant", () => selectedCommander?.TryAssign(1));
            CreateButton(rect, 120f, "Return 1", () => selectedCommander?.ReturnTroops(1));
            CreateButton(rect, 228f, "Next Role", CycleRole);
            CreateButton(rect, 336f, "Next Rank", CycleRank);
            // 스킬 버튼은 클릭 시점의 선택을 다시 조회한다(캐시된 selectedCommander를 쓰지 않는다).
            powerStrikeText = CreateButton(rect, 348f, "Strike",
                () => FindSingleSelectedCommander(selection)?.TryPowerStrike(), 96f);
            stanceText = CreateButton(rect, 348f, "Guard",
                () => FindSingleSelectedCommander(selection)?.TryDefensiveStance(), 64f);
            panel.SetActive(false);
        }

        private void LateUpdate()
        {
            if (panel == null) return;
            int count = 0;
            float current = 0f;
            float maximum = 0f;
            AntUnitBase first = null;
            selectedCommander = null;
            if (selection != null)
            {
                foreach (var selectable in selection.GetSelectedObjects())
                {
                    if (selectable == null || !selectable.isActiveAndEnabled || !selectable.IsSelected) continue;
                    var unit = selectable.GetComponent<AntUnitBase>();
                    if (unit == null || !unit.isActiveAndEnabled || unit.IsDead || unit.Data == null) continue;
                    if (first == null) first = unit;
                    count++;
                    current += unit.CurrentHealth;
                    maximum += unit is CommanderAnt commander ? commander.CommandLimit : unit.Data.maxHealth;
                }
            }
            panel.SetActive(count > 0);
            if (count == 0) return;
            selectedCommander = count == 1 ? first as CommanderAnt : null;
            powerStrikeText.transform.parent.gameObject.SetActive(selectedCommander != null);
            stanceText.transform.parent.gameObject.SetActive(selectedCommander != null);
            title.text = selectedCommander != null ? $"{selectedCommander.CommanderName} ({selectedCommander.Rank})" : $"Selected Commanders: {count}";
            healthText.text = $"HP {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";
            combatStatsText.text = count != 1
                ? ""
                : first.Data.role == UnitRole.Worker
                    ? $"Armor {first.Armor:0.#}"
                    : $"ATK {first.AttackDamage:0.#}   Armor {first.Armor:0.#}";
            healthFill.anchorMax = new Vector2(maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f, 1f);
            workText.text = "";
            if (selectedCommander == null) return;
            var work = selectedCommander.WorkProficiency;
            var workProgress = work.Level >= CommanderWorkProficiency.MaxLevel ? "MAX"
                : $"{Mathf.FloorToInt(work.Progress)}/{CommanderWorkProficiency.UnitsPerLevel:0}";
            workText.text = $"Gather Lv {work.Level} ({workProgress}) +{Mathf.RoundToInt((work.GatherMultiplier - 1f) * 100f)}% speed";
            var progression = selectedCommander.Progression;
            var xp = progression.XpToNext > 0 ? $"{progression.Xp}/{progression.XpToNext}" : "MAX";
            combatStatsText.text = $"{selectedCommander.Role}  " + combatStatsText.text + $"   Lv {progression.Level} (XP {xp})";

            var skills = selectedCommander.Skills;
            powerStrikeText.GetComponentInParent<Button>().interactable = selectedCommander.CanPowerStrike;
            stanceText.GetComponentInParent<Button>().interactable = selectedCommander.CanDefensiveStance;
            powerStrikeText.text = SkillLabel("Strike", CommanderSkills.PowerStrikeLevel, progression.Level,
                skills.PowerStrikeArmed ? "ON" : null, skills.PowerStrikeCooldownLeft);
            stanceText.text = SkillLabel("Guard", CommanderSkills.DefensiveStanceLevel, progression.Level,
                skills.DefensiveStanceActive ? $"ON {Mathf.CeilToInt(skills.DefensiveStanceTimeLeft)}s" : null,
                skills.DefensiveStanceCooldownLeft);
        }

        // 잠김(Lv n) → 발동 중(ON) → 재사용 대기(초) → 준비 순으로 표시한다.
        private static string SkillLabel(string name, int requiredLevel, int level, string active, float cooldown)
        {
            if (level < requiredLevel) return $"{name} Lv{requiredLevel}";
            if (active != null) return $"{name} {active}";
            return cooldown > 0f ? $"{name} {Mathf.CeilToInt(cooldown)}s" : name;
        }

        private void CycleRole()
        {
            if (selectedCommander == null) return;
            var roles = selectedCommander.AllowedRoles;
            for (var i = 0; i < roles.Count; i++)
                if (roles[i] == selectedCommander.Role)
                {
                    selectedCommander.TrySetRole(roles[(i + 1) % roles.Count]);
                    return;
                }
        }

        private void CycleRank()
        {
            if (selectedCommander == null) return;
            var count = System.Enum.GetValues(typeof(CommanderRank)).Length;
            for (var offset = 1; offset < count; offset++)
                if (selectedCommander.TrySetRank((CommanderRank)(((int)selectedCommander.Rank + offset) % count))) return;
        }

        private static Text CreateButton(Transform parent, float x, string label, UnityEngine.Events.UnityAction action, float y = 30f)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(100f, 26f);
            go.GetComponent<Image>().color = new Color(.2f, .3f, .2f);
            go.GetComponent<Button>().onClick.AddListener(action);
            var text = CreateText(label, rect, Vector2.zero);
            text.text = label;
            text.rectTransform.sizeDelta = rect.sizeDelta;
            text.alignment = TextAnchor.MiddleCenter;
            return text;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(string name, Transform parent, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.raycastTarget = false;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(276f, 24f);
            return text;
        }
    }
}

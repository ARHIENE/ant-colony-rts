using System;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;
using UnityEngine.UI;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        private Action refreshDutyScreen;
        private static readonly CommanderJobs[] DutyJobs = { CommanderJobs.Nursing, CommanderJobs.Repair, CommanderJobs.Cleaning, CommanderJobs.Building, CommanderJobs.Art, CommanderJobs.Crafting,
            CommanderJobs.Research, CommanderJobs.Administration, CommanderJobs.Cooking, CommanderJobs.Hunting, CommanderJobs.Hauling, CommanderJobs.Husbandry, CommanderJobs.Farming, CommanderJobs.Fishing, CommanderJobs.Gathering };
        private static readonly string[] DutyNames = { "간호", "수리", "치우기", "건설", "예술", "제작", "연구", "행정", "요리", "사냥", "운반", "사육", "농사", "낚시", "채집" };

        public void WorkSchedule()
        {
            var f = Frame("작업표");
            var p = L.Plate(f, "WorkSchedule", 180, 100, 1080, 650);
            L.Label(p, "작업표", 26, 24, 12, 1032, 40, MenuTheme.Accent);
            L.Label(p, "체크 해제 = 금지. 아래 숫자를 눌러 우선순위 1(매우 낮음)~5(매우 높음)를 바꿉니다. 높은 숫자부터, 같으면 왼쪽부터 처리합니다. 진행 중인 작업은 마친 뒤 적용합니다.", 14, 24, 58, 1032, 42, MenuTheme.Muted);
            L.Label(p, "장수 / 현재 상태", 14, 24, 110, 210, 32);
            for (var i = 0; i < DutyJobs.Length; i++) L.Label(p, DutyNames[i], 13, 244 + i * 52, 110, 57, 32);
            var list = L.List(p, 24, 148, 1032, 426);
            foreach (var c in SortedCommanders())
            {
                var row = L.Cell(list, "Duty " + c.CommanderName, 62, MenuTheme.Plate2);
                var label = L.Label(row, "", 14, 8, 0, 204, 62);
                refreshDutyScreen += () => label.text = c != null ? c.CommanderName + " · " + Status(c) : "이탈한 장수";
                for (var i = 0; i < DutyJobs.Length; i++)
                {
                    var job = DutyJobs[i];
                    var toggle = DutyToggle(row, "Job " + job, 220 + i * 52, 4, c.AllowsJob(job));
                    var level = L.Button(row, "Priority " + job, "", 220 + i * 52, 35, 28, 22, () => { if (c != null && c.AllowsJob(job)) c.SetJobPriority(job, c.JobPriority(job) % CommanderWorkState.MaxPriority + 1); refreshDutyScreen?.Invoke(); });
                    var levelText = level.GetComponentInChildren<Text>();
                    toggle.onValueChanged.AddListener(on => { if (c != null) c.SetJobEnabled(job, on); refreshDutyScreen?.Invoke(); });
                    var skill = job == CommanderJobs.Hunting ? (c.Role == AntColony.Data.UnitRole.Ranged ? CommanderActivity.Ranged : CommanderActivity.Melee) : CommanderAnt.SkillFor(job);
                    var skillText = L.Label(row, "", 11, 248 + i * 52, 2, 33, 58, MenuTheme.Accent);
                    refreshDutyScreen += () => {
                        toggle.interactable = c != null && !c.IsDead && c.CanDoJob(job);
                        if (c != null) skillText.text = job == CommanderJobs.Cleaning ? (c.Traits.Has(CommanderTrait.Neat) ? "×1.5" : "—")
                            : c.CanDoJob(job) ? c.Talents.Level(skill) + new string('♥', c.Traits.Flame(skill)) : "불가";
                        if (c != null) toggle.SetIsOnWithoutNotify(c.AllowsJob(job));
                        level.interactable = c != null && c.AllowsJob(job);
                        if (c != null) levelText.text = c.AllowsJob(job) ? c.JobPriority(job).ToString() : "—";
                    };
                }
            }
            L.Button(p, "Forbid Gathering", "채집 금지", 24, 592, 150, 36, () => { Resume(); GatherDesignation.Begin(true); });
            L.Button(p, "Clear Designation", "지정 취소", 186, 592, 150, 36, () => { Resume(); GatherDesignation.Begin(false); });
            L.Button(p, "Close Duty", "게임 재개", 856, 592, 200, 36, Resume);
            refreshDutyScreen?.Invoke();
        }

        private static Toggle DutyToggle(Transform parent, string name, float x, float y, bool value)
        {
            var box = L.Box(parent, name, x, y, 28, 28, MenuTheme.Well, true);
            var check = L.Label(box, "✓", 22, 0, 0, 28, 28, MenuTheme.Accent, TextAnchor.MiddleCenter);
            var toggle = box.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = box.GetComponent<Image>(); toggle.graphic = check;
            toggle.isOn = value;
            return toggle;
        }
    }
}

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
        private static readonly CommanderJobs[] DutyJobs = { CommanderJobs.Firefighting, CommanderJobs.Nursing, CommanderJobs.Repair, CommanderJobs.Cleaning, CommanderJobs.Building, CommanderJobs.Art, CommanderJobs.Crafting,
            CommanderJobs.Research, CommanderJobs.Administration, CommanderJobs.Cooking, CommanderJobs.Hunting, CommanderJobs.Hauling, CommanderJobs.Husbandry, CommanderJobs.Farming, CommanderJobs.Fishing, CommanderJobs.Gathering };
        private static readonly string[] DutyNames = { "소화", "의료", "수리", "치우기", "건설", "예술", "제작", "연구", "행정", "요리", "사냥", "운반", "사육", "농사", "낚시", "채집" };

        private int dutyPage;
        public void WorkSchedule()
        {
            var p = PrototypeWindow("작업표", "좌클릭 +1 · 우클릭 −1 · 0 금지 · 높은 숫자 우선 · 아래 숫자 기술 / ♥ 열정", WorkSchedule);
            float width = p.sizeDelta.x, height = p.sizeDelta.y;
            int count = Mathf.Clamp(Mathf.FloorToInt((width - 240) / 49), 1, DutyJobs.Length);
            dutyPage = Mathf.Clamp(dutyPage, 0, (DutyJobs.Length - 1) / count);
            int first = dutyPage * count, last = Mathf.Min(DutyJobs.Length, first + count);
            L.Label(p, "장수 / 현재 상태", 14, 20, 108, 170, 28);
            L.Button(p, "Previous Duties", "◀", width - 106, 78, 38, 28, () => { dutyPage--; WorkSchedule(); }).interactable = dutyPage > 0;
            L.Button(p, "Next Duties", "▶", width - 60, 78, 38, 28, () => { dutyPage++; WorkSchedule(); }).interactable = last < DutyJobs.Length;
            L.Label(p, $"작업 {first + 1}–{last} / {DutyJobs.Length}", 12, 20, 78, 250, 24, MenuTheme.Muted);
            for (var i = first; i < last; i++) L.Label(p, DutyNames[i], 13, 200 + (i-first) * 49, 108, 49, 28);
            var list = L.List(p, 20, 142, width-40, height-204);
            foreach (var c in SortedCommanders())
            {
                var row = L.Cell(list, "Duty " + c.CommanderName, 62, MenuTheme.Plate2);
                var label = L.Label(row, "", 14, 8, 0, 164, 62);
                refreshDutyScreen += () => label.text = c != null ? c.CommanderName + " · " + Status(c) : "이탈한 장수";
                for (var i = first; i < last; i++)
                {
                    var job = DutyJobs[i];
                    void Cycle(int direction)
                    {
                        if (c == null || c.IsDead || !c.CanDoJob(job)) return;
                        var current = c.AllowsJob(job) ? c.JobPriority(job) : 0;
                        var next = (current + direction + CommanderWorkState.MaxPriority + 1) % (CommanderWorkState.MaxPriority + 1);
                        c.SetJobEnabled(job, next != 0);
                        if (next != 0) c.SetJobPriority(job, next);
                        refreshDutyScreen?.Invoke();
                    }
                    var level = L.Button(row, "Priority " + job, "", 180 + (i-first) * 49, 4, 45, 30, () => Cycle(1));
                    var clicks = level.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                    var right = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick };
                    right.callback.AddListener(e => { if (((UnityEngine.EventSystems.PointerEventData)e).button == UnityEngine.EventSystems.PointerEventData.InputButton.Right) Cycle(-1); });
                    clicks.triggers.Add(right);
                    var levelText = level.GetComponentInChildren<Text>();
                    var skill = job == CommanderJobs.Hunting ? (c.Role == AntColony.Data.UnitRole.Ranged ? CommanderActivity.Ranged : CommanderActivity.Melee) : CommanderAnt.SkillFor(job);
                    var skillText = L.Label(row, "", 12, 180 + (i-first) * 49, 35, 45, 24, MenuTheme.Muted);
                    var trainingTip = skillText.gameObject.AddComponent<MenuTooltip>();
                    refreshDutyScreen += () => {
                        level.interactable = c != null && !c.IsDead && c.CanDoJob(job);
                        if (c != null) skillText.text = job == CommanderJobs.Cleaning ? (c.Traits.Has(CommanderTrait.Neat) ? "×1.5" : "—")
                            : c.CanDoJob(job) ? c.Talents.Level(skill) + new string('♥', c.Traits.Flame(skill)) : "불가";

                        if (c != null) levelText.text = c.AllowsJob(job) ? c.JobPriority(job).ToString() : "—";
                        if (c != null) trainingTip.Message = job == CommanderJobs.Cleaning ? "치우기는 기술 경험치를 지급하지 않습니다." : CommanderDevelopment.TrainingText(c.Talents, skill);
                    };
                }
            }
            L.Button(p, "Forbid Gathering", "채집 금지", 20, height-46, 140, 32, () => { Resume(); GatherDesignation.Begin(true); });
            L.Button(p, "Clear Designation", "작업 지정·취소", 170, height-46, 140, 32, WorkDesignations);
            L.Button(p, "Select Tasks", "작업 선택", 320, height-46, 140, 32, () => { Resume(); GatherDesignation.BeginTaskSelect(); }); // 드래그로 일괄 관리(2026-10-12)
            L.Button(p, "All Tasks", "전체 작업", 470, height-46, 140, 32, () => ShowTasks(System.Linq.Enumerable.ToList(TaskCandidates())));


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

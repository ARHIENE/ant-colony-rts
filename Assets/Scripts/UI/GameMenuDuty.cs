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
        private static readonly CommanderJobs[] DutyJobs = { CommanderJobs.Building, CommanderJobs.Crafting,
            CommanderJobs.Research, CommanderJobs.Farming, CommanderJobs.Fishing, CommanderJobs.Gathering };
        private static readonly string[] DutyNames = { "건설", "제작", "연구", "농사", "낚시", "채집" };

        public void WorkSchedule()
        {
            var f = Frame("작업표");
            var p = L.Plate(f, "WorkSchedule", 180, 100, 1080, 650);
            L.Label(p, "작업표", 26, 24, 12, 1032, 40, MenuTheme.Accent);
            L.Label(p, "왼쪽부터 우선 처리합니다. 진행 중인 작업은 마친 뒤 변경 사항을 적용합니다. 출전 중에는 자율 작업을 멈춥니다.", 14, 24, 58, 1032, 42, MenuTheme.Muted);
            L.Label(p, "장수 / 현재 상태", 14, 24, 110, 280, 32);
            for (var i = 0; i < DutyJobs.Length; i++) L.Label(p, DutyNames[i], 14, 340 + i * 112, 110, 100, 32);
            var list = L.List(p, 24, 148, 1032, 426);
            foreach (var c in SortedCommanders())
            {
                var row = L.Cell(list, "Duty " + c.CommanderName, 44, MenuTheme.Plate2);
                var label = L.Label(row, "", 14, 8, 0, 300, 44);
                refreshDutyScreen += () => label.text = c != null ? c.CommanderName + " · " + Status(c) : "이탈한 장수";
                for (var i = 0; i < DutyJobs.Length; i++)
                {
                    var job = DutyJobs[i];
                    var toggle = DutyToggle(row, "Job " + job, 316 + i * 112, 8, c.AllowsJob(job));
                    toggle.onValueChanged.AddListener(on => { if (c != null) c.SetJobEnabled(job, on); });
                    refreshDutyScreen += () => {
                        toggle.interactable = c != null && !c.IsDead;
                        if (c != null) toggle.SetIsOnWithoutNotify(c.AllowsJob(job));
                    };
                }
            }
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

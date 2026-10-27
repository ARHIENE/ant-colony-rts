using AntColony.World;
using UnityEngine;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        public void WorkDesignations()
        {
            var f = Frame("작업 지정");
            var p = L.Plate(f, "WorkDesignations", 330, 180, 780, 460);
            L.Label(p, "작업 지정", 21, 24, 16, 720, 36, bold: true);
            L.Label(p, "대상을 클릭·드래그로 지정하면 가능한 장수가 자동으로 작업합니다.\n미지정 자연 자원은 캐지 않으며 산출물은 운반 작업으로 회수합니다.", 15, 24, 62, 720, 58);
            void Pick(string name, string label, float x, float y, NaturalWorkKind kind, bool cancel = false)
                => L.Button(p, name, label, x, y, 222, 40, () => { Resume(); GatherDesignation.BeginWork(kind, cancel); });
            Pick("Designate Logging", "벌목 지정", 24, 140, NaturalWorkKind.Logging);
            Pick("Designate Mining", "채굴 지정", 278, 140, NaturalWorkKind.Mining);
            Pick("Designate Harvesting", "야생 채집 지정", 532, 140, NaturalWorkKind.Harvesting);
            Pick("Cancel Natural Work", "자연 작업 지정 취소", 24, 196, NaturalWorkKind.None, true);
            L.Button(p, "Forbid Natural Work", "채집 금지", 278, 196, 222, 40, () => { Resume(); GatherDesignation.Begin(true); });
            L.Button(p, "Allow Natural Work", "채집 금지 해제", 532, 196, 222, 40, () => { Resume(); GatherDesignation.Begin(false); });
            L.Button(p, "Work Orders", "전체 작업·대기 이유", 24, 260, 350, 40, () => ShowTasks(System.Linq.Enumerable.ToList(TaskCandidates())));
            L.Button(p, "Work Priorities", "대상 우선순위 지정", 406, 260, 350, 40, () => { Resume(); GatherDesignation.BeginPriority(); });
            L.Label(p, "취소는 남은 지정 작업만 해제합니다. 이미 나온 자원은 남습니다.\n밭·낚시·생산 주문은 기존 설정에 따라 계속 자동 작업합니다.", 14, 24, 322, 720, 62, MenuTheme.Muted);
            L.Button(p, "Close Work Designations", "닫기", 642, 400, 112, 36, Resume);
        }
    }
}

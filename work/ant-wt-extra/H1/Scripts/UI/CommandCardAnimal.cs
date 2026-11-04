using AntColony.World;
using UnityEngine;

namespace AntColony.UI
{
    public sealed partial class CommandCard
    {
        private RectTransform animalGrid;
        private static Critter Animal => WorkTargetPanel.Target != null ? WorkTargetPanel.Target.GetComponent<Critter>() : null;

        private void BuildAnimalCommands()
        {
            animalGrid = Grid("AnimalCommands", 3);
            Add(animalGrid, 0, "Animal Capture", () => Animal.CaptureDesignated ? "포획 취소" : "포획 요청", () => {
                var a = Animal;
                if (a != null && !a.Bound && a.Clinic == null && a.Carrier == null) a.CaptureDesignated = !a.CaptureDesignated;
            }, () => "사육 장수가 포획한 뒤 현장에 결박합니다.", () => !Animal.Bound && Animal.Clinic == null && Animal.Carrier == null);
            Add(animalGrid, 1, "Animal Carry", () => Animal.TransportTo != null ? "운반 취소" : "운반 요청", () => {
                var a = Animal; if (a == null) return;
                if (a.TransportTo != null) { a.TransportTo = null; a.Carrier?.DropCarriedCritter(); }
                else GameMenuController.Instance?.OpenCritterTab(a, false);
            }, () => "결박·쓰러짐 상태에서 받을 우리를 선택합니다.", () => Animal.Carriable || Animal.TransportTo != null);
            Add(animalGrid, 2, "Animal Slaughter", () => Animal.SlaughterStarted ? "도축 중" : Animal.SlaughterOrdered ? "도축 취소" : "도축 요청", () => {
                var a = Animal; if (a == null || a.SlaughterStarted) return;
                if (a.SlaughterOrdered) a.CancelSlaughter();
                else GameMenuController.Instance?.ConfirmCritterAction(a, false);
            }, () => Animal.SlaughterBlock ?? "이름을 확인한 뒤 도축을 요청합니다.", () => !Animal.SlaughterStarted && (Animal.SlaughterOrdered || Animal.SlaughterBlock == null));
            Add(animalGrid, 3, "Animal Treat", () => "치료 지시", () => GameMenuController.Instance?.OpenCritterTab(Animal, true),
                () => "투약·수술 상태와 의료 재고를 확인합니다.", () => Animal.Injured || Animal.Clinic != null || Animal.Recovering);
            Add(animalGrid, 4, "Animal Care", () => Animal.CareOrder ? "돌봄 지시됨" : "돌봄 1회", () => {
                var a = Animal; if (a != null && a.InOperatingPen) a.CareOrder = true;
            }, () => "운영 중인 우리에서 사육 장수가 돌봅니다.", () => Animal.InOperatingPen && !Animal.CareOrder);
            Add(animalGrid, 5, "Animal Rename", () => "이름 변경", () => GameMenuController.Instance?.OpenCritterTab(Animal, false), () => "개체 이름을 변경합니다.");
            Add(animalGrid, 6, "Animal Details", () => "상세", () => GameMenuController.Instance?.ShowCritter(Animal), () => "개체 상태와 요청을 확인합니다.");
            Add(animalGrid, 7, "Animal Release", () => "방생", () => GameMenuController.Instance?.ConfirmCritterAction(Animal, true),
                () => "이름 확인 후 즉시 사라집니다. 치료 시설 안에서는 불가합니다.", () => Animal.CanRelease);
            Add(animalGrid, 8, "Animal Clear", () => "선택 해제", () => { selection?.ClearSelection(); WorkTargetPanel.Clear(); }, () => "선택을 해제합니다.");
        }
    }
}

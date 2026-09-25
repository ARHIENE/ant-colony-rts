using AntColony.Core;
using AntColony.Buildings;
using AntColony.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace AntColony.Units
{
    // 선택 자체는 SelectionManager(드래그/클릭/Shift 추가선택)가 담당하고,
    // 이 스크립트는 선택된 SoldierAnt들에게 우클릭으로 이동/공격 명령만 내린다.
    public class UnitSelectionController : MonoBehaviour
    {
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private SelectionManager selectionManager;
        [SerializeField] private float formationSpacing = 1.5f;
        [SerializeField] private Color moveMarkerColor = Color.green;
        [SerializeField] private BuildingPlacementController buildingPlacementController;

        private UnityEngine.Camera cam;
        private AttackMoveController attackMoveController;

        private void Awake()
        {
            cam = UnityEngine.Camera.main;
            attackMoveController = FindFirstObjectByType<AttackMoveController>();
            if (selectionManager == null) selectionManager = FindFirstObjectByType<SelectionManager>();
            if (buildingPlacementController == null) buildingPlacementController = FindFirstObjectByType<BuildingPlacementController>();
        }

        private void Update()
        {
            if (AntColony.UI.GameMenuController.BlocksInput) return;
            if (AntColony.UI.SkillTargeting.ConsumesPointerInput) return;
            var mouse = Mouse.current;
            if (mouse == null || selectionManager == null) return;
            if (buildingPlacementController != null && buildingPlacementController.ConsumesPointerInput) return;
            if (attackMoveController != null && attackMoveController.ConsumesPointerInput) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (mouse.rightButton.wasPressedThisFrame)
            {
                IssueCommand(mouse.position.ReadValue());
            }
        }

        private void IssueCommand(Vector2 screenPos)
        {
            if (cam == null) return;

            var selected = selectionManager.GetSelectedObjects();
            if (selected.Count == 0) return;

            var ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out var lootHit, 500f, ~0) && lootHit.collider.GetComponentInParent<EquipmentLoot>() is EquipmentLoot loot)
            {
                foreach (var selectable in selected)
                    if (selectable != null && loot.TryCollect(selectable.GetComponent<CommanderAnt>())) return;
                AntColony.UI.ToastManager.Show("장비 회수 불가: 유휴 장수와 보관함 빈칸이 필요합니다.");
                return;
            }
            // 파손된 함정 우클릭: 선택된 장수 한 명이 수리하러 간다.
            foreach (var trapHit in Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Collide))
            {
                var trap = trapHit.collider.GetComponentInParent<AntColony.Buildings.TrapPit>();
                if (trap == null || trap.Armed) continue;
                foreach (var selectable in selected)
                    if (selectable != null && selectable.GetComponent<CommanderAnt>() is CommanderAnt repairer && trap.TryRepair(repairer))
                    {
                        MoveMarker.Spawn(trap.Position, moveMarkerColor);
                        return;
                    }
            }
            if (!Physics.Raycast(ray, out var hit, 500f, groundMask)) return;

            // 적(IDamageable, 야생 몬스터/보스 등)을 직접 클릭하면 전원 그 타겟을 공격.
            var target = hit.collider.GetComponentInParent<IDamageable>();
            if (target is BuildingBase building && building.CountsTowardPlayerDefeat)
            {
                target = null;
            }
            // 자원노드를 클릭하면 일개미는 그 자리로 이동해 채집을 시작한다.
            var resourceNode = hit.collider.GetComponentInParent<ResourceNode>();
            var deposit = hit.collider.GetComponentInParent<BuildingBase>();

            var cols = Mathf.CeilToInt(Mathf.Sqrt(selected.Count));
            var index = 0;
            var issuedMove = false;

            foreach (var selectable in selected)
            {
                if (selectable == null) continue;

                var col = index % cols;
                var row = index / cols;
                var offset = new Vector3((col - (cols - 1) / 2f) * formationSpacing, 0f, row * -formationSpacing);

                // 장수/일개미도 SoldierAnt를 상속하므로 채집 지시를 먼저 판정한다.
                var worker = selectable.GetComponent<WorkerAnt>();
                if (worker != null && worker.TryReturnCargo(deposit))
                {
                    issuedMove = true;
                    index++;
                    continue;
                }
                if (worker != null && resourceNode != null && resourceNode.CanGather)
                {
                    worker.CommandGather(resourceNode);
                    issuedMove = true;
                    index++;
                    continue;
                }

                var soldier = selectable.GetComponent<SoldierAnt>();
                if (soldier != null)
                {
                    // 대공 불가 역할이나 채집 보직이 공격 대상을 클릭하면 공격 대신 그 위치로 이동만 한다.
                    if (target != null && soldier.CanAttackTarget(target))
                    {
                        soldier.CommandAttack(target);
                    }
                    else
                    {
                        soldier.CommandMove(hit.point + offset);
                        issuedMove = true;
                    }
                    index++;
                }
            }

            if (issuedMove)
            {
                MoveMarker.Spawn(hit.point, moveMarkerColor);
            }
        }
    }
}

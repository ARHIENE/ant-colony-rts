using System.Linq;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Buildings
{
    // 철거·가구 이동(2026-10-08 기획): 둘 다 건설 예정지처럼 장수가 현장에서 작업해야 끝난다(작업표 '건설', 대상 우선순위·노란 경보 적용).
    // 철거는 건설비의 70%를 반환하고, 이동은 추가 비용 없이 새 위치로 옮긴다. 벽·문·바닥·기둥·전선은 이동하지 않고 철거 후 다시 짓는다.
    public static class Demolition
    {
        public static bool Pending(BuildingBase b) => Object.FindObjectsByType<DemolitionSite>(FindObjectsSortMode.None).Any(s => s.Target == b);
        public static bool CanDemolish(BuildingBase b) => b != null && !b.IsDead && b.Data != null && b.CountsTowardPlayerDefeat && !(b is Stockpile) && !Pending(b);
        public static bool CanMove(BuildingBase b) => CanDemolish(b) && !RoomSystem.IsBoundary(b)
            && b.Data.kind != BuildingKind.Floor && b.Data.kind != BuildingKind.Pillar && b.Data.kind != BuildingKind.PowerWire;

        public static BuildingConstructionSite OrderDemolish(BuildingBase b)
        {
            if (!CanDemolish(b)) return null;
            return Site(b, b.Position, false);
        }

        // 새 위치 검증은 배치 컨트롤러(BeginMove)가 한다.
        public static BuildingConstructionSite OrderMove(BuildingBase b, Vector3 to)
        {
            if (!CanMove(b)) return null;
            return Site(b, to, true);
        }

        private static BuildingConstructionSite Site(BuildingBase b, Vector3 at, bool move)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = b.name + (move ? " 이동 예정지" : " 철거 예정지");
            var size = b.GetComponent<Renderer>() is Renderer r ? r.bounds.size : Vector3.one * 2f;
            go.transform.position = new Vector3(at.x, b.Position.y - size.y * .5f + .1f, at.z);
            go.transform.localScale = new Vector3(size.x, .2f, size.z);
            go.GetComponent<Collider>().isTrigger = true;
            go.GetComponent<Renderer>().material.color = move ? new Color(.3f, .6f, .9f) : new Color(.85f, .3f, .25f);
            var site = go.AddComponent<BuildingConstructionSite>();
            site.Initialize(null, Mathf.Max(1f, b.Data.buildTimeSeconds * (move ? GameBalance.MoveWorkShare : GameBalance.DemolishWorkShare)));
            var mark = go.AddComponent<DemolitionSite>(); mark.Target = b; mark.Move = move; mark.MoveTo = at;
            return site;
        }
    }

    // 철거·이동 예정지 표식. 대상이 사라지면 예정지도 없앤다. 건설 현장처럼 저장 전에 끝내야 한다.
    public sealed class DemolitionSite : MonoBehaviour
    {
        public BuildingBase Target { get; internal set; }
        public bool Move { get; internal set; }
        public Vector3 MoveTo { get; internal set; }

        internal void Finish()
        {
            if (Target == null) return;
            if (Move)
            {
                Target.transform.position = new Vector3(MoveTo.x, Target.Position.y, MoveTo.z);
                AntColony.UI.ToastManager.Show(Target.Data.displayName + " 이동 완료");
            }
            else
            {
                var d = Target.Data; var share = GameBalance.DemolishRefundShare;
                AntColony.World.DiplomacyManager.StoreResource(ResourceType.Food, Mathf.FloorToInt(d.foodCost * share), ResourceReason.Refund);
                AntColony.World.DiplomacyManager.StoreResource(ResourceType.Soil, Mathf.FloorToInt(d.soilCost * share), ResourceReason.Refund);
                AntColony.World.DiplomacyManager.StoreResource(ResourceType.Special, Mathf.FloorToInt(d.specialCost * share), ResourceReason.Refund);
                AntColony.UI.ToastManager.Show(d.displayName + " 철거 완료");
                Target.gameObject.SetActive(false); Destroy(Target.gameObject);
            }
            RoomSystem.MarkDirty(); Target = null;
        }

        private void Update() { if (Target == null || Target.IsDead) Destroy(gameObject); }
    }
}

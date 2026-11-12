using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Core
{
    public sealed partial class ColonyPopulation
    {
        private readonly Dictionary<int, NavMeshPath> civilianPaths = new Dictionary<int, NavMeshPath>();
        private readonly Dictionary<int, GameObject> civilianViews = new Dictionary<int, GameObject>();
        public const int CivilianGroupLimit = 50;

        public void ReturnRocketCivilians(int count, int young, int old, Vector3 position)
        {  }

        public int BeginCivilianEvacuation(int sourceId, Vector3 destination, int requested = CivilianGroupLimit)
        { return 0; }

        // 좌석을 먼저 예약하지 않는다. 호출자는 도착 생존자와 빈 좌석 수를 확인한 뒤 이 반환값만 탑승 처리한다.
        public int TakeArrivedCivilians(int groupId, int seats)
        { return 0; }

        private void TickCivilianLocations(float seconds)
        {
            foreach (var group in S.civilianLocations)
            {
                if ((!group.moving && !group.arrived) || group.count == 0) continue;
                if (!civilianViews.TryGetValue(group.id, out var view))
                {
                    view = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    view.GetComponent<Collider>().enabled = false;
                    view.transform.localScale = new Vector3(.6f, .6f, .6f);
                    view.AddComponent<AntColony.Units.CivilianVisual>();
                    civilianViews[group.id] = view;
                }
                view.name = $"시민 대피 #{group.id} ({group.count}명)";
                view.transform.position = group.position + Vector3.up * .6f;
                if (!group.moving) continue;
                // 경로는 저장하지 않는다. 복원 및 동적 장애물 변경에 대응해 현재 위치에서 다시 계산한다.
                if (!civilianPaths.TryGetValue(group.id, out var path)) civilianPaths[group.id] = path = new NavMeshPath();
                if (!NavMesh.CalculatePath(group.position, group.destination, NavMesh.AllAreas, path)
                    || path.status != NavMeshPathStatus.PathComplete) continue;
                var distance = seconds * 3f;
                foreach (var corner in path.corners)
                {
                    var leg = Vector3.Distance(group.position, corner);
                    group.position = Vector3.MoveTowards(group.position, corner, distance);
                    distance = Mathf.Max(0, distance - leg);
                    if (distance == 0) break;
                }
                if ((group.position - group.destination).sqrMagnitude < .01f)
                {
                    group.position = group.destination; group.moving = false; group.arrived = true;
                    civilianPaths.Remove(group.id);
                }
                view.transform.position = group.position + Vector3.up * .6f;
            }
            foreach (var id in new List<int>(civilianViews.Keys))
                if (!S.civilianLocations.Exists(l => l.id == id && l.count > 0 && (l.moving || l.arrived)))
                { Destroy(civilianViews[id]); civilianViews.Remove(id); civilianPaths.Remove(id); }
            SettleArrivals();
            S.civilianLocations.RemoveAll(l => l.count == 0 && !l.housing);
        }

        private void ClearCivilianViews()
        {
            foreach (var view in civilianViews.Values) if (view != null) Destroy(view);
            civilianViews.Clear();
        }
    }
}

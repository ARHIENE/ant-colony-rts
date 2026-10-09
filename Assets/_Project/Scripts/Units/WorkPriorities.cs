using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AntColony.Units
{
    // 작업 대상별 우선순위(산소미포함식 1~9, 9 최우선, 기본 5)와 노란 경보(그 대상을 최우선). 기본값이면 붙이지 않는다.
    public sealed class TargetPriority : MonoBehaviour
    {
        public int level = WorkPriorities.Default;
        public bool yellow;
    }

    // 장수 작업 선택 순서: 노란 경보 대상 → 장수별 작업 종류 우선순위 → 대상 우선순위 → 거리.
    // 빨간 경보: 소굴 전체. 식사·수면·치료받기·간호(와 휴식·씻기·놀기)를 멈추고 작업한다. 작업 금지는 유지.
    public static class WorkPriorities
    {
        public const int Min = 1, Max = 9, Default = 5;
        public static bool Red { get; private set; }

        [RuntimeInitializeOnLoadMethod]
        private static void Hook() => SceneManager.sceneLoaded += (_, __) => Red = false; // 새 게임은 경보 해제 상태(불러오기는 Restore가 다시 켠다)

        public static int Level(Component c) => c != null && c.TryGetComponent<TargetPriority>(out var p) ? p.level : Default;
        public static bool Yellow(Component c) => c != null && c.TryGetComponent<TargetPriority>(out var p) && p.yellow;
        public static void Set(Component c, int level, bool yellow)
        {
            if (c == null) return;
            level = Mathf.Clamp(level, Min, Max);
            var p = c.GetComponent<TargetPriority>();
            if (level == Default && !yellow) { if (p != null) { p.level = Default; p.yellow = false; UnityEngine.Object.Destroy(p); } return; }
            if (p == null) p = c.gameObject.AddComponent<TargetPriority>();
            p.level = level; p.yellow = yellow;
        }
        public static void SetRed(bool on)
        {
            Red = on;
            AntColony.UI.ToastManager.SetCrisis("red-alert", on ? "빨간 경보: 장수 전원이 식사·수면·치료·간호를 멈추고 작업합니다" : null);
        }
        // 후보를 노란 경보만 / 대상 우선순위 높은 순 → 가까운 순으로 정렬한다.
        public static IEnumerable<T> Rank<T>(IEnumerable<T> items, Vector3 from, bool yellowOnly) where T : Component
            => items.Where(i => i != null && (!yellowOnly || Yellow(i))).OrderByDescending(Level).ThenBy(i => (i.transform.position - from).sqrMagnitude);

        // 우선순위를 지정할 수 있는 대상: 자원 노드·건물(건설 예정지 포함)·시체·야생 개체.
        public static IEnumerable<Component> Candidates()
            => ResourceNode.Available.Cast<Component>()
                .Concat(UnityEngine.Object.FindObjectsByType<BuildingBase>(FindObjectsSortMode.None))
                .Concat(UnityEngine.Object.FindObjectsByType<BuildingConstructionSite>(FindObjectsSortMode.None))
                .Concat(Corpse.All).Concat(WildMonster.All.Where(m => m.Huntable)).Where(c => c != null);

        [Serializable] public sealed class Entry { public Vector3 position; public int level; public bool yellow; }
        [Serializable] public sealed class State { public bool red; public List<Entry> targets = new List<Entry>(); }
        // ponytail: 대상은 저장 시 위치로 기억해 불러온 뒤 같은 자리(0.3m)의 대상에 다시 붙인다. 겹친 대상이 생기면 저장 식별자로 바꾼다.
        public static State Capture() => new State
        {
            red = Red,
            targets = UnityEngine.Object.FindObjectsByType<TargetPriority>(FindObjectsSortMode.None)
                .Where(p => p.GetComponent<BuildingConstructionSite>() == null)
                .Select(p => new Entry { position = p.transform.position, level = p.level, yellow = p.yellow }).ToList()
        };
        public static void Restore(State s)
        {
            SetRed(s?.red == true);
            if (s?.targets == null) return;
            var candidates = Candidates().ToList();
            foreach (var e in s.targets)
            {
                var target = candidates.Where(c => (c.transform.position - e.position).sqrMagnitude < .09f).OrderBy(c => (c.transform.position - e.position).sqrMagnitude).FirstOrDefault();
                if (target != null) Set(target, e.level, e.yellow);
            }
        }
    }
}

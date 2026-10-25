using System;
using System.Collections.Generic;
using AntColony.Buildings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AntColony.Core
{
    // G26: 바닥 묶음 1만 개가 있으면 FindObjectsByType 한 번이 씬 전체(약 3.6만 컴포넌트)를 훑어 16ms가 든다.
    // 건물은 BuildingBase가, 자주 찾는 그 밖의 형식은 OnEnable/OnDisable에서 등록해 목록에서 꺼낸다. 등록 안 된 형식만 기존 검색.
    public static class SceneObjects
    {
        private static readonly List<BuildingBase> Buildings = new List<BuildingBase>();
        // 목록에 있는 형식은 인스턴스가 하나도 없어도 검색하지 않는다.
        private static readonly Dictionary<Type, List<Object>> Registered = new Dictionary<Type, List<Object>>
        {
            [typeof(BuildingConstructionSite)] = new List<Object>(), [typeof(AntColony.Boss.BossHealth)] = new List<Object>(),
            [typeof(AntColony.World.EventActor)] = new List<Object>(), [typeof(AntColony.World.EnemyColony)] = new List<Object>(),
            [typeof(PrisonerCamp)] = new List<Object>(), [typeof(RedevelopmentSite)] = new List<Object>(), [typeof(DemolitionSite)] = new List<Object>(),
            [typeof(WallFire)] = new List<Object>(), [typeof(AntColony.World.EquipmentLoot)] = new List<Object>(), [typeof(AntColony.Units.TargetPriority)] = new List<Object>(),
        };

        internal static void AddBuilding(BuildingBase b) => Buildings.Add(b);

        // T는 All<T>로 찾을 형식(파생형은 기반형으로 등록).
        public static void Register<T>(T item) where T : Object
        {
            if (!Registered.TryGetValue(typeof(T), out var list)) Registered[typeof(T)] = list = new List<Object>();
            list.Add(item);
        }
        public static void Unregister<T>(T item) where T : Object
        {
            if (Registered.TryGetValue(typeof(T), out var list)) list.Remove(item);
        }

        // FindObjectsByType(FindObjectsSortMode.None)와 같은 결과: 활성 오브젝트, 순서 없음, 새 배열.
        public static T[] All<T>() where T : Object
        {
            var result = new List<T>();
            if (typeof(BuildingBase).IsAssignableFrom(typeof(T)))
            {
                Buildings.RemoveAll(b => b == null); // 파괴된 건물
                foreach (var b in Buildings) if (b is T t && b != null && b.gameObject.activeInHierarchy) result.Add(t);
                return result.ToArray();
            }
            // 야생 개체·자원 노드는 각자 켜진 목록을 이미 가지고 있다.
            if (typeof(AntColony.World.WildMonster).IsAssignableFrom(typeof(T)))
            {
                foreach (var m in AntColony.World.WildMonster.All) if (m is T t && m != null) result.Add(t);
                return result.ToArray();
            }
            if (typeof(T) == typeof(AntColony.World.ResourceNode))
            {
                foreach (var n in AntColony.World.ResourceNode.Available) if (n != null) result.Add(n as T);
                return result.ToArray();
            }
            if (!Registered.TryGetValue(typeof(T), out var list)) return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
            foreach (var o in list) if (o != null && (!(o is Component c) || c.gameObject.activeInHierarchy)) result.Add((T)o);
            return result.ToArray();
        }

        // FindFirstObjectByType/FindAnyObjectByType 대체. 씬에 하나뿐인 관리자·UI는 찾은 것을 살아 있는 동안 다시 쓴다.
        private static readonly Dictionary<Type, Object> AnyCache = new Dictionary<Type, Object>();
        public static T Any<T>() where T : Object
        {
            if (typeof(BuildingBase).IsAssignableFrom(typeof(T)) || Registered.ContainsKey(typeof(T)))
            {
                var all = All<T>(); return all.Length > 0 ? all[0] : null;
            }
            if (AnyCache.TryGetValue(typeof(T), out var cached) && cached != null && (!(cached is Component c) || c.gameObject.activeInHierarchy)) return (T)cached;
            var found = Object.FindAnyObjectByType<T>();
            AnyCache[typeof(T)] = found;
            return found;
        }
    }
}

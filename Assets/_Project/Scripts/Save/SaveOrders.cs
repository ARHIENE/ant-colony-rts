using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.Save
{
    [Serializable] public class SoldierOrderDto
    {
        public int state;
        public string target;
        public float attackTimer, searchTimer;
        public bool attackMove, moving;
        public Vec3Dto destination = new Vec3Dto(), attackDestination = new Vec3Dto();
    }
    [Serializable] public class WorkerOrderDto
    {
        public int state, fetchType, fetchAmount;
        public string node, deposit, construction, delivery, fetchHost, fishingNode;
        public bool fetchBase, depositBin;
        public float buildTimer;
    }
    [Serializable] public class CommanderOrderDto
    {
        public SoldierOrderDto soldier;
        public WorkerOrderDto worker;
        public string service, hunt, automaticFacility;
        public int serviceJob;
        public float huntCooldown, workScan;
    }
    [Serializable] public class WildMonsterDto
    {
        public bool commander, raider, wasp, night, flying, allied, docile, provoked, visitor;
        public string name, rebel, target, commanderName;
        public int parentSite = -1, raidSite = -1, invasionSite = -2, defenseSite = -1, rank;
        public List<int> roles = new List<int>();
        public float maxHealth, detection, damage, range, interval, searchInterval, speed, attackTimer, searchTimer, siegeCheck, root;
        public float captureChance, rotationY;
        public Vec3Dto scale = new Vec3Dto(Vector3.one);
    }

    // Keys are shared by the existing building/node schema and active orders. No scene references enter JSON.
    internal static class SaveReferences
    {
        private static readonly Dictionary<string, Component> objects = new Dictionary<string, Component>();
        private static readonly Dictionary<Component, string> keys = new Dictionary<Component, string>();
        internal static void Clear() { objects.Clear(); keys.Clear(); }
        internal static void Register(string key, Component value)
        {
            if (value == null) return;
            objects[key] = value;
            if (!(value is ResourceNode) || !keys.TryGetValue(value, out var old) || !old.StartsWith("node:") || key.StartsWith("node:")) keys[value] = key;
        }
        internal static string Key(object value)
        {
            if (!(value is Component c) || c == null) return null;
            if (keys.TryGetValue(c, out var key)) return key;
            foreach (var component in c.GetComponents<Component>())
                if (keys.TryGetValue(component, out key)) return key;
            throw new InvalidOperationException("Unregistered save target: " + c.name + " / " + c.GetType().Name);
        }
        internal static T Get<T>(string key) where T : class
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (!objects.TryGetValue(key, out var c)) throw new InvalidOperationException("Missing save target: " + key);
            if (c == null) return null;
            var target = c as T ?? c.GetComponent(typeof(T)) as T;
            if (target == null) throw new InvalidOperationException("Wrong saved target type: " + key + " / " + typeof(T).Name);
            return target;
        }
        internal static void Building(string key, BuildingBase b)
        {
            Register("building:" + key, b);
            if (b == null) return;
            var nodes = b.GetComponentsInChildren<ResourceNode>(true);
            for (int i = 0; i < nodes.Length; i++) Register("building:" + key + ":node:" + i, nodes[i]);
        }
        internal static void Colonies()
        {
            var world = WorldMapManager.Instance;
            for (int i = -1; i < world.Sites.Count; i++)
            {
                var colony = i < 0 ? UnityEngine.Object.FindObjectsByType<EnemyColony>(FindObjectsSortMode.None)
                    .FirstOrDefault(c => c.GetComponentInParent<ExpeditionSite>() == null) : world.Sites[i].Colony;
                if (colony == null) continue;
                for (int j = 0; j < colony.Buildings.Length; j++) Building("colony:" + i + ":" + j, colony.Buildings[j]);
            }
        }
        internal static void Actors()
        {
            foreach (var actor in SaveCatalog.Ordered<EventActor>())
                Register("event:" + (int)actor.Kind + ":" + JsonUtility.ToJson(new Vec3Dto(actor.transform.position)), actor);
            foreach (var critter in Critter.All) Register("critter:" + critter.Id, critter.Monster);
            foreach (var rebel in WildMonster.All.Where(m => !string.IsNullOrEmpty(m.RebelId))) Register("rebel:" + rebel.RebelId, rebel);
        }
    }
}

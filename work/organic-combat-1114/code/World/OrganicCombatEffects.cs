using System;
using System.Collections.Generic;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    [Serializable]
    public sealed class OrganicEffectState
    {
        public int mode, species, ticks;
        public bool allied;
        public string target;
        public Vector3 start, end;
        public float left, duration, damage, radius;
    }

    // 발사된 공격은 발사자가 죽어도 끝까지 진행한다. 장면 수명과 함께 소멸하고 저장에는 남은 시간만 기록한다.
    public sealed class OrganicCombatEffects : MonoBehaviour
    {
        sealed class Effect
        {
            public OrganicEffectState state;
            public IDamageable target;
            public GameObject visual, marker;
        }
        readonly List<Effect> effects = new List<Effect>();
        Material material;
        public int PendingCount => effects.Count;
        static OrganicCombatEffects Find() => SceneObjects.Any<OrganicCombatEffects>();
        public static OrganicCombatEffects Ensure() => Find() ?? new GameObject("OrganicCombatEffects").AddComponent<OrganicCombatEffects>();
        public static bool Opponent(IDamageable target, bool allied)
        {
            if (!CombatTargeting.IsAlive(target) || CombatTargeting.IsAirborne(target)) return false;
            if (target is WildMonster monster) return monster.Allied != allied;
            return !allied && (target is AntUnitBase || target is BuildingBase);
        }
        public static BuildingBase BlockingBarrier(Vector3 from, Vector3 to, IDamageable target)
        {
            var delta = to - from;
            float distance = delta.magnitude;
            BuildingBase nearest = null; float best = distance;
            foreach (var hit in Physics.RaycastAll(from + Vector3.up * .45f, delta.normalized, distance, ~0, QueryTriggerInteraction.Ignore))
            {
                var building = hit.collider.GetComponentInParent<BuildingBase>();
                if (building == null || building.IsDead || ReferenceEquals(building, target) || !WildMonster.IsBarrier(building) || hit.distance >= best) continue;
                best = hit.distance; nearest = building;
            }
            return nearest;
        }
        public static void Fire(WildMonster source, IDamageable target, Vector3 aim, float damage)
        {
            if (BlockingBarrier(source.Position, aim, target) is BuildingBase barrier) { target = barrier; aim = barrier.Position; }
            float duration = Mathf.Clamp(Vector3.Distance(source.Position, aim) / 9, .18f, 1.3f);
            Ensure().Add(new OrganicEffectState { species = (int)source.OrganicKind, allied = source.Allied,
                start = source.Position, end = aim, left = duration, duration = duration, damage = damage,
                radius = OrganicEnemyRules.Splash(source.OrganicKind) }, target);
        }
        public static void Corrode(WildMonster source, IDamageable target, float damage)
        {
            if (!(target is BuildingBase) || !CombatTargeting.IsAlive(target)) return;
            Ensure().Add(new OrganicEffectState { mode = 1, species = (int)OrganicEnemyKind.WallMelter, allied = source.Allied,
                start = source.Position, end = target.Position, damage = damage * .25f, ticks = 4, left = 1, duration = 1 }, target);
        }
        GameObject Dot(string name, Color color, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = name;
            go.transform.SetParent(transform); go.transform.localScale = Vector3.one * size;
            var collider = go.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            if (material == null) material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = material;
            var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", color); renderer.SetPropertyBlock(block);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }
        void Add(OrganicEffectState state, IDamageable target)
        {
            var effect = new Effect { state = state, target = target };
            var color = state.mode == 1 ? new Color(.45f, 1, .15f) : new Color(.85f, .55f, .2f);
            effect.visual = Dot(state.mode == 1 ? "부식" : "생체 투사체", color, state.mode == 1 ? .35f : .16f);
            if (state.radius > 0)
            {
                effect.marker = Dot("착탄 예고", new Color(.9f, .35f, .15f), state.radius * 2);
                effect.marker.transform.localScale = new Vector3(state.radius * 2, .025f, state.radius * 2);
                effect.marker.transform.position = state.end + Vector3.up * .03f;
            }
            effects.Add(effect); Pose(effect);
        }
        void Pose(Effect effect)
        {
            var s = effect.state;
            float t = 1 - Mathf.Clamp01(s.left / Mathf.Max(.001f, s.duration));
            effect.visual.transform.position = s.mode == 1 ? s.end + Vector3.up * .2f
                : Vector3.Lerp(s.start, s.end, t) + Vector3.up * (.45f + (s.radius > 1 ? Mathf.Sin(t * Mathf.PI) * 1.3f : 0));
        }
        void Update() { if (!SaveSystem.Busy) Tick(Time.deltaTime); }
        public void Tick(float dt)
        {
            if (!(dt > 0) || float.IsInfinity(dt)) return;
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                var effect = effects[i]; var s = effect.state; s.left -= dt;
                if (s.mode == 1)
                {
                    while (s.left <= 0 && s.ticks > 0 && Opponent(effect.target, s.allied))
                    { FormationDamage.Apply(effect.target, s.damage, s.start, area: true); s.ticks--; s.left += 1; }
                    if (s.ticks <= 0 || !Opponent(effect.target, s.allied)) { Remove(i); continue; }
                }
                else if (s.left <= 0) { Impact(effect); Remove(i); continue; }
                Pose(effect);
            }
        }
        void Impact(Effect effect)
        {
            var s = effect.state; var target = effect.target;
            if (BlockingBarrier(s.start, s.end, target) is BuildingBase barrier) target = barrier;
            if (s.radius > 0)
            {
                Vector3 center = target is BuildingBase b && WildMonster.IsBarrier(b) && (b.Position - s.end).sqrMagnitude > 1 ? b.Position : s.end;
                var targets = new HashSet<IDamageable>();
                foreach (var hit in Physics.OverlapSphere(center, s.radius, ~0, QueryTriggerInteraction.Ignore))
                {
                    var candidate = hit.GetComponentInParent<IDamageable>();
                    if (!Opponent(candidate, s.allied) || !targets.Add(candidate) || BlockingBarrier(center, candidate.Position, candidate) != null) continue;
                    FormationDamage.Apply(candidate, s.damage, s.start, ranged: true, area: true);
                }
            }
            else if (Opponent(target, s.allied) && (target is BuildingBase || Vector3.Distance(target.Position, s.end) <= .75f))
            {
                FormationDamage.Apply(target, s.damage, s.start, ranged: true);
                if (s.species == (int)OrganicEnemyKind.HookMaw && target is CommanderAnt commander && !commander.IsDead)
                    Pull(commander, s.start);
            }
        }
        static void Pull(CommanderAnt target, Vector3 toward)
        {
            var agent = target.GetComponent<NavMeshAgent>();
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
            var delta = toward - target.Position; delta.y = 0;
            if (delta.sqrMagnitude < .01f) return;
            var destination = target.Position + delta.normalized * Mathf.Min(2, delta.magnitude);
            if (!NavMesh.SamplePosition(destination, out var point, .4f, agent.areaMask)
                || NavMesh.Raycast(target.Position, point.position, out _, agent.areaMask)
                || BlockingBarrier(target.Position, point.position, target) != null) return;
            target.CommandStop(); agent.Warp(point.position);
        }
        void Remove(int index)
        {
            Destroy(effects[index].visual); if (effects[index].marker != null) Destroy(effects[index].marker);
            effects.RemoveAt(index);
        }
        public static List<OrganicEffectState> Capture()
        {
            var result = new List<OrganicEffectState>(); var runner = Find();
            if (runner == null) return result;
            foreach (var effect in runner.effects)
            {
                var state = JsonUtility.FromJson<OrganicEffectState>(JsonUtility.ToJson(effect.state));
                state.target = CombatTargeting.IsAlive(effect.target) ? SaveReferences.Key(effect.target) : null;
                result.Add(state);
            }
            return result;
        }
        public static void Restore(List<OrganicEffectState> states)
        {
            var runner = Find();
            if (runner != null) while (runner.effects.Count > 0) runner.Remove(runner.effects.Count - 1);
            if (states == null || states.Count == 0) return;
            runner = runner ?? Ensure();
            foreach (var state in states)
            {
                var copy = JsonUtility.FromJson<OrganicEffectState>(JsonUtility.ToJson(state));
                runner.Add(copy, SaveReferences.Get<IDamageable>(copy.target));
            }
        }
        void OnDestroy() { if (material != null) Destroy(material); }
    }
}

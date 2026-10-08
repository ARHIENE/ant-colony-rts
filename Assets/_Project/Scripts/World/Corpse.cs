using System;
using System.Collections.Generic;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    public enum CorpseKind { Ant, ColonyCommander, EnemyCommander, Wildlife }

    // 병력 손실 한 번을 한 묶음으로 표시한다. 시체 수만큼 먹을 수 있지만 청소는 묶음 전체를 처리한다.
    public sealed class Corpse : MonoBehaviour
    {
        public const float Lifetime = 300, WorkSeconds = 5;
        public static readonly List<Corpse> All = new List<Corpse>();
        [Serializable] public class State
        {
            public string name;
            public Vector3 position;
            public CorpseKind kind;
            public int count = 1;
            public float remaining = Lifetime, food;
            public bool priority, gatheringForbidden;
            public int workforce;
            public string workerId;
            public bool eating;
            public float progress;
        }
        public State Data { get; private set; }
        public CommanderAnt Handler { get; private set; }
        public Vector3 Position => transform.position;
        public bool Available => isActiveAndEnabled && Data.count > 0 && Data.remaining > 0;
        public bool Edible => Available && Data.kind != CorpseKind.Wildlife;
        public bool Priority { get => Data.priority; set => Data.priority = value; }
        private void OnEnable() => All.Add(this);
        private void OnDisable() { All.Remove(this); Handler?.CommandStop(); Handler = null; }
        private void Update() { if (!AntColony.Save.SaveSystem.Busy) Tick(Time.deltaTime); }
        public void Tick(float seconds)
        {
            if (!Available || !(seconds > 0) || float.IsInfinity(seconds)) return;
            Data.remaining = Mathf.Max(0, Data.remaining - seconds);
            if (Data.remaining == 0) Remove();
        }
        public bool Claim(CommanderAnt commander)
        {
            if (!Available || commander == null || Handler != null && Handler != commander) return false;
            Handler = commander; return true;
        }
        public void Release(CommanderAnt commander) { if (Handler == commander) Handler = null; }
        public bool Finish(CommanderAnt commander, bool eat)
        {
            if (!Available || Handler != commander || eat && (!Edible || !commander.Traits.Has(CommanderTrait.Cannibal))) return false;
            Handler = null;
            if (eat && --Data.count > 0) return true;
            Remove(); return true;
        }
        private void Remove() { gameObject.SetActive(false); Destroy(gameObject); }
        public State Capture()
        {
            var state = JsonUtility.FromJson<State>(JsonUtility.ToJson(Data)); state.position = Position;
            var food = GetComponent<ResourceNode>(); state.food = food != null ? food.AmountRemaining : 0;
            state.gatheringForbidden = food != null && food.GatheringForbidden;
            state.workerId = Handler != null ? Handler.PersonalState.id : null;
            state.eating = Handler != null && Handler.EatingCorpse;
            state.progress = Handler != null ? Handler.CorpseProgress : 0;
            return state;
        }
        public static Corpse Drop(Component source, CorpseKind kind, string name, int count = 1, float food = 0)
        {
            if (count <= 0) return null;
            var body = source.GetComponent<AntVisual>()?.Death(true);
            var position = source.transform.position;
            if (UnityEngine.AI.NavMesh.SamplePosition(position, out var ground, 32, UnityEngine.AI.NavMesh.AllAreas)) position = ground.position;
            return Spawn(new State { position = position,
                kind = kind, name = name, count = count, food = food }, body);
        }
        public static Corpse Spawn(State state, GameObject body = null)
        {
            var root = new GameObject(state.food > 0 ? "사냥 사체" : state.name + " 시체"); root.SetActive(false);
            root.transform.position = state.position;
            var corpse = root.AddComponent<Corpse>(); corpse.Data = JsonUtility.FromJson<State>(JsonUtility.ToJson(state));
            var collider = root.AddComponent<BoxCollider>(); collider.center = Vector3.up * .25f; collider.size = new Vector3(1.4f, .5f, 1.4f);
            bool freshDeath = body != null;
            if (body == null)
            {
                var ant = state.kind != CorpseKind.Wildlife ? Resources.Load<GameObject>("QuirkyAnt") : null;
                body = ant != null ? Instantiate(ant) : GameObject.CreatePrimitive(PrimitiveType.Cube);
                if (ant == null) body.transform.localScale = new Vector3(.9f, .2f, .6f);
            }
            body.transform.SetParent(root.transform, true); body.transform.position = state.position;
            foreach (var c in body.GetComponentsInChildren<Collider>()) c.enabled = false;
            if (state.food > 0)
            {
                var node = root.AddComponent<ResourceNode>(); node.ConfigureLoot(ResourceType.Food, state.food);
                node.GatheringForbidden = state.gatheringForbidden;
            }
            root.SetActive(true);
            var animator = body.GetComponentInChildren<Animator>();
            if (animator != null) { animator.applyRootMotion = false; animator.Play("Base Layer.Death", 0, freshDeath ? 0 : 1); animator.Update(0); animator.speed = freshDeath ? 1 : 0; }
            return corpse;
        }
    }
}

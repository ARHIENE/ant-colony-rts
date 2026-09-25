using System;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    public enum EventActorKind { Wasp, Wanderer, Food, Soil }
    public sealed class EventActor : MonoBehaviour
    {
        [Serializable] public class State
        {
            public EventActorKind kind;
            public Vec3Dto position = new Vec3Dto();
            public float remaining, amount, attackCooldown;
        }
        public EventActorKind Kind { get; private set; }
        public float Remaining { get; private set; }
        public bool Alive => Kind != EventActorKind.Wasp || GetComponent<WildMonster>() is WildMonster m && !m.IsDead;
        public static EventActor Spawn(State state)
        {
            var go = GameObject.CreatePrimitive(state.kind == EventActorKind.Food || state.kind == EventActorKind.Soil ? PrimitiveType.Cube : PrimitiveType.Capsule);
            go.SetActive(false); go.name = "Event " + state.kind; go.transform.position = state.position.ToVector3();
            go.transform.localScale = Vector3.one * (state.kind == EventActorKind.Wasp ? .8f : 1.2f);
            var actor = go.AddComponent<EventActor>(); actor.Kind = state.kind; actor.Remaining = state.remaining;
            var color = state.kind == EventActorKind.Wasp ? new Color(1, .65f, .1f) : state.kind == EventActorKind.Wanderer ? Color.cyan
                : state.kind == EventActorKind.Food ? AntColony.Boss.BossLoot.FoodColor : new Color(.5f, .3f, .15f);
            var properties = new MaterialPropertyBlock(); properties.SetColor("_BaseColor", color); properties.SetColor("_Color", color);
            go.GetComponent<Renderer>().SetPropertyBlock(properties);
            if (state.kind == EventActorKind.Wasp)
            {
                var monster = go.AddComponent<WildMonster>(); monster.ConfigureEventWasp(); go.SetActive(true);
                monster.RestoreHealth(state.amount); monster.EventAttackCooldown = state.attackCooldown;
            }
            else if (state.kind == EventActorKind.Food || state.kind == EventActorKind.Soil)
            {
                var node = go.AddComponent<ResourceNode>(); node.ConfigureLoot(state.kind == EventActorKind.Food ? ResourceType.Food : ResourceType.Soil, state.amount);
                go.SetActive(true);
            }
            else go.SetActive(true);
            return actor;
        }
        public State Capture() => new State { kind = Kind, position = new Vec3Dto(transform.position), remaining = Remaining,
            amount = Kind == EventActorKind.Wasp ? GetComponent<WildMonster>().CurrentHealth : GetComponent<ResourceNode>()?.AmountRemaining ?? 0,
            attackCooldown = Kind == EventActorKind.Wasp ? Mathf.Max(0, GetComponent<WildMonster>().EventAttackCooldown) : 0 };
        public void Tick(float seconds)
        {
            if (!isActiveAndEnabled || !Alive || !(seconds > 0) || float.IsInfinity(seconds) || Kind == EventActorKind.Wasp) return;
            // 경과 후 접근은 영입 기회가 아니다.
            Remaining = Mathf.Max(0, Remaining - seconds);
            if (Remaining == 0) { Expire("기한 만료"); return; }
            if (Kind != EventActorKind.Wanderer || CommanderRoster.Instance == null) return;
            var visitor = CommanderRoster.Instance.Commanders.FirstOrDefault(c => c.CanReceiveOrders && !c.IsAwayFromHome
                && Vector3.Distance(c.Position, transform.position) <= EventRules.ApproachRadius);
            if (visitor == null) return;
            var post = FindFirstObjectByType<ScoutPost>();
            var chance = Mathf.Clamp01((post != null ? post.CurrentChance : ScoutPost.RecruitmentChance(CommanderRoster.Instance.Count)) + EventRules.RecruitBonus);
            CommanderAnt recruit = null;
            if (CommanderRoster.Instance.Count < ScoutPost.DefaultMaxCommanders && UnityEngine.Random.value < chance)
                recruit = CommanderRoster.Instance.Create(null, CommanderRank.Corporal, new[] { UnitRole.Worker }, UnitRole.Worker,
                    CommanderTraits.Random(), transform.position);
            if (recruit != null) CampaignHistory.Record("합류", recruit.CommanderName, "방랑 장수 영입");
            Expire(recruit == null ? "영입 제안을 거절하고 떠났습니다." : recruit.CommanderName + " 합류");
        }
        private void Expire(string result)
        {
            CampaignHistory.Record("이벤트 결과", Kind == EventActorKind.Wanderer ? "장수 후보 방랑" : "표류물", result, true);
            gameObject.SetActive(false); Destroy(gameObject);
        }
    }
}

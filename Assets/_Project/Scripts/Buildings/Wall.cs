using System.Linq;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Buildings
{
    // Phase 5 벽 재료(잠정): 나뭇잎·나무껍질 벽(싸고 빠르지만 불에 탐) / 병뚜껑 벽(비싸고 단단) / 성벽(두껍고 튼튼, 근처 장수 방어 +2).
    // 흙벽은 기존 SoilWall을 그대로 쓴다. NavMesh 장애물(템플릿)이 통행을 막는다.
    public sealed class Wall : BuildingBase
    {
        private static readonly System.Collections.Generic.List<Wall> Castle = new System.Collections.Generic.List<Wall>();
        public static System.Collections.Generic.IReadOnlyList<Wall> CastleWalls => Castle;
        protected override void OnEnable() { base.OnEnable(); if (IsCastle) Castle.Add(this); }
        protected override void OnDisable() { Castle.Remove(this); base.OnDisable(); }
        public bool IsCastle => Data != null && Data.kind == BuildingKind.CastleWall;
        public bool Flammable => Data != null && Data.kind == BuildingKind.LeafWall;
        public override float Armor => Data == null ? 0 : Data.kind switch
        {
            BuildingKind.LeafWall => 0, BuildingKind.CapWall => GameBalance.WallArmor + 2, BuildingKind.CastleWall => GameBalance.WallArmor + 4, _ => GameBalance.WallArmor
        };
        protected override bool UsesDefenseDurability => true;
    }

    // 나뭇잎 벽 불(2026-10-03, 잠정): 적이 불을 지르면 몇 초 동안 계속 타고, 2초 뒤 바로 옆 나뭇잎 벽으로 확률 번짐. 벽마다 한 번만 탄다.
    public sealed class WallFire : MonoBehaviour
    {
        private Wall wall;
        private float remaining, tick, spreadIn = 2f;
        public bool Burning => remaining > 0;
        public static bool Ignite(Wall target)
        {
            if (target == null || !target.Flammable || target.IsDead || target.GetComponent<WallFire>() != null) return false;
            var fire = target.gameObject.AddComponent<WallFire>();
            fire.wall = target; fire.remaining = GameBalance.WallFireSeconds;
            if (target.GetComponent<Renderer>() is Renderer r) r.material.color = new Color(.95f, .45f, .1f, 1);
            return true;
        }
        private void Update()
        {
            if (!Burning || wall == null) return;
            remaining -= Time.deltaTime;
            if ((spreadIn -= Time.deltaTime) <= 0 && spreadIn > -Time.deltaTime)
                foreach (var hit in Physics.OverlapSphere(wall.Position, 1.6f, ~0, QueryTriggerInteraction.Ignore))
                    if (hit.GetComponentInParent<Wall>() is Wall next && next != wall && Random.value < GameBalance.WallFireSpreadChance) Ignite(next);
            if ((tick += Time.deltaTime) < 1f) return;
            tick -= 1f; wall.TakeDamage(GameBalance.WallFireDamagePerSecond);
        }
    }

    // 문: 아군은 지나가고 적은 막힌다(잠정: 문에 닿은 적은 밀려나며 문을 공격한다). 방 판정에서는 벽처럼 경계.
    // 잠금문(2026-10-05)은 밤에 아군도 못 지나간다. 창살문·잠금문은 감옥 판정에 쓰인다.
    public sealed class Door : BuildingBase
    {
        private float scan;
        private NavMeshObstacle lockObstacle;
        public bool IsPrisonDoor => Data != null && (Data.kind == BuildingKind.LockedDoor || Data.kind == BuildingKind.BarredDoor);
        public bool Locked => lockObstacle != null && lockObstacle.enabled;
        private void Update()
        {
            if (Data != null && Data.kind == BuildingKind.LockedDoor)
            {
                if (lockObstacle == null) { lockObstacle = gameObject.AddComponent<NavMeshObstacle>(); lockObstacle.carving = true; }
                lockObstacle.enabled = GameCalendar.IsNight;
            }
            if ((scan -= Time.deltaTime) > 0) return;
            scan = .25f;
            foreach (var m in AntColony.World.WildMonster.All.ToArray())
            {
                if (m == null || m.IsDead || !m.isActiveAndEnabled) continue;
                var d = m.transform.position - Position; d.y = 0;
                if (d.sqrMagnitude > 1.2f * 1.2f) continue;
                var push = d.sqrMagnitude < .01f ? Vector3.forward : d.normalized;
                var agent = m.GetComponent<NavMeshAgent>();
                if (agent != null && agent.enabled) agent.Warp(Position + push * 1.4f); else m.transform.position = Position + push * 1.4f;
                TakeDamage(GameBalance.DoorBashDamage);
            }
        }
    }

    // 성문: 열면 모두 통과, 닫으면 성벽처럼 막는다. 상태는 저장한다.
    public sealed class Gate : BuildingBase
    {
        [SerializeField] private bool open = true;
        public bool Open => open;
        public override float Armor => GameBalance.WallArmor + 4;
        protected override bool UsesDefenseDurability => true;
        protected override void OnEnable() { base.OnEnable(); Apply(); }
        public void SetOpen(bool value) { open = value; Apply(); }
        private void Apply()
        {
            var obstacle = GetComponent<NavMeshObstacle>();
            if (obstacle != null) obstacle.enabled = !open;
            var r = GetComponent<Renderer>();
            if (r != null) r.material.color = open ? new Color(.55f, .45f, .3f, 1) : new Color(.35f, .27f, .18f, 1);
        }
    }
}

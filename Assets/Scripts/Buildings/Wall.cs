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

    // 문: 아군은 지나가고 적은 막힌다(잠정: 문에 닿은 적은 밀려나며 문을 공격한다). 방 판정에서는 벽처럼 경계.
    public sealed class Door : BuildingBase
    {
        private float scan;
        private void Update()
        {
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

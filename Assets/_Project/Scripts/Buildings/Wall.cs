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
        protected override void OnEnable() { base.OnEnable(); if (IsCastle) Castle.Add(this); FitObstacle(this); }
        // NavMesh 장애물은 에이전트 반경(0.5)만큼 넓게 깎이므로 벽 칸 크기 그대로면 1칸 문·2칸 성문 틈까지 막혔다(2026-10-11 수정).
        // 깎이는 폭이 벽 칸보다 0.1m씩만 넓도록 장애물을 줄인다. 이웃한 벽끼리는 여전히 이어져 막힌다.
        internal static void FitObstacle(BuildingBase b)
        {
            var obstacle = b.GetComponent<NavMeshObstacle>(); if (obstacle == null) return;
            var radius = NavMesh.GetSettingsCount() > 0 ? NavMesh.GetSettingsByIndex(0).agentRadius : .5f; var scale = b.transform.lossyScale;
            float Local(float world, float s) => Mathf.Max(.05f, world - 2 * radius + .2f) / Mathf.Max(.01f, Mathf.Abs(s));
            obstacle.shape = NavMeshObstacleShape.Box; obstacle.center = Vector3.zero;
            obstacle.size = new Vector3(Local(Mathf.Abs(scale.x), scale.x), obstacle.size.y, Local(Mathf.Abs(scale.z), scale.z));
        }
        protected override void OnDisable() { Castle.Remove(this); base.OnDisable(); }
        public bool IsCastle => Data != null && Data.kind == BuildingKind.CastleWall;
        public bool Flammable => Data != null && Data.kind == BuildingKind.LeafWall;
        public override float Armor => Data == null ? 0 : Data.kind switch
        {
            BuildingKind.LeafWall => 0, BuildingKind.CapWall => GameBalance.WallArmor + 2, BuildingKind.CastleWall => GameBalance.WallArmor + 4, _ => GameBalance.WallArmor
        };
        protected override bool UsesDefenseDurability => true;
    }

    // 불(2026-10-03 나뭇잎 벽, 2026-10-10 가연성 재료 전체): 적이 불을 지르면 몇 초 동안 계속 타고, 2초 뒤 바로 옆 가연성 건물로 확률 번짐. 건물마다 한 번만 탄다.
    public sealed class WallFire : MonoBehaviour
    {
        [System.Serializable] public sealed class State
        {
            public float remaining, tick, spreadIn = 2f, extinguishWork;
            public Color originalColor = Color.white;
        }
        private BuildingBase wall;
        private State state = new State();
        private void OnEnable() => AntColony.Core.SceneObjects.Register(this);
        private void OnDisable() => AntColony.Core.SceneObjects.Unregister(this);
        public const float ExtinguishSeconds = 5f;
        public bool Burning => state.remaining > 0 && wall != null && !wall.IsDead;
        public float ExtinguishProgress => state.extinguishWork / ExtinguishSeconds;
        // 별도 구역 편집이 생기기 전까지 본거지 내 아군 건물 영역을 관리 구역으로 쓴다.
        public bool Managed => wall != null && wall.CountsTowardPlayerDefeat
            && AntColony.Map.HomeMapBuilder.CurrentWorldBounds.Contains(new Vector3(wall.Position.x, 0, wall.Position.z));
        public State CaptureState() => JsonUtility.FromJson<State>(JsonUtility.ToJson(state));
        public void RestoreState(State saved)
        {
            wall = GetComponent<BuildingBase>(); state = saved ?? new State();
            if (GetComponent<Renderer>() is Renderer r) r.material.color = Burning ? new Color(.95f, .45f, .1f, 1) : state.originalColor;
        }
        public bool Extinguish(float seconds)
        {
            if (!Burning || !(seconds > 0) || float.IsInfinity(seconds)) return false;
            state.extinguishWork = Mathf.Min(ExtinguishSeconds, state.extinguishWork + seconds);
            if (state.extinguishWork >= ExtinguishSeconds) { state.remaining = 0; RestoreColor(); }
            return Burning;
        }
        private void RestoreColor() { if (GetComponent<Renderer>() is Renderer r) r.material.color = state.originalColor; }
        public static bool Flammable(BuildingBase b) => b != null && (b.GetComponent<FarmPlot>() != null || MaterialInfo.For(SpaceQuality.MaterialOf(b)).flammable);
        public static bool Ignite(BuildingBase target)
        {
            if (target == null || !Flammable(target) || target.IsDead) return false;
            var fire = target.GetComponent<WallFire>();
            if (fire != null && fire.Burning) return false;
            if (fire == null) fire = target.gameObject.AddComponent<WallFire>();
            fire.wall = target;
            fire.state = new State { remaining = GameBalance.WallFireSeconds,
                originalColor = target.GetComponent<Renderer>() is Renderer renderer ? renderer.material.color : Color.white };
            if (target.GetComponent<Renderer>() is Renderer r) r.material.color = new Color(.95f, .45f, .1f, 1);
            return true;
        }
        private void Update()
        {
            if (!Burning || wall == null) return;
            state.remaining = Mathf.Max(0, state.remaining - Time.deltaTime);
            if (!Burning) { RestoreColor(); return; }
            if ((state.spreadIn -= Time.deltaTime) <= 0 && state.spreadIn > -Time.deltaTime && !ScienceEffects.FirebreaksBlockSpread)
                foreach (var hit in Physics.OverlapSphere(wall.Position, 1.6f, ~0, QueryTriggerInteraction.Ignore))
                    if (hit.GetComponentInParent<BuildingBase>() is BuildingBase next && next != wall && Random.value < GameBalance.WallFireSpreadChance) Ignite(next);
            if ((state.tick += Time.deltaTime) < 1f) return;
            state.tick -= 1f; wall.TakeDamage(GameBalance.WallFireDamagePerSecond * ScienceEffects.WildfireDamageMultiplier * BiomeRules.WildfireDamage);
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
        public bool OpenNow => !Locked && AntColony.Units.AntUnitBase.Active.Any(u => u is AntColony.Units.CommanderAnt c && !c.IsHostile && (c.Position - Position).sqrMagnitude < 2.25f);
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
                // 목장 생물(2026-10-11): 문을 직접 열지 못하지만 아군이 지나며 문이 열린 동안엔 통과한다. 부수지는 않는다.
                if (m.Docile && OpenNow) continue;
                var push = d.sqrMagnitude < .01f ? Vector3.forward : d.normalized;
                var agent = m.GetComponent<NavMeshAgent>();
                if (agent != null && agent.enabled) agent.Warp(Position + push * 1.4f); else m.transform.position = Position + push * 1.4f;
                if (!m.Docile) TakeDamage(GameBalance.DoorBashDamage);
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

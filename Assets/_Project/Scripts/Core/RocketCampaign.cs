using System;
using System.Linq;
using AntColony.Buildings;
using UnityEngine;
using AntColony.Units;
using AntColony.World;

namespace AntColony.Core
{
    // 씬과 함께 수명이 끝난다. 건물 파괴와 무관한 위기 유예·경제 이력·엔딩·본거지 기준점.
    [DefaultExecutionOrder(-1000)]
    public sealed class RocketCampaign : MonoBehaviour
    {
        [Serializable] public sealed class State
        {
            public float crisisResumeAt;
            public RocketEconomy economy = new RocketEconomy();
            public CampaignOutcome.State ending;
            public bool hasHome;
            public Vector3 home;
            public int initialCommanders;
            public bool assaultStarted;
            public float assaultAt, assaultRemaining;
            public int assaultWave;
        }
        public static RocketCampaign Instance { get; private set; }
        private State state = new State();
        public RocketEconomy Economy => state.economy;
        public CampaignOutcome.State Ending => state.ending;
        public Vector3 Home => state.home;
        public bool HasHome => state.hasHome;
        public int InitialCommanders => state.initialCommanders;
        public static bool Operating => AntColony.Core.SceneObjects.All<AirshipYard>().Any(y => y.Operating && !y.IsDead);
        public static bool SuppressNewCrises => Operating || Instance != null && GameCalendar.GameSeconds < Instance.state.crisisResumeAt;
        // 생성기는 이 이벤트에 연결한다. 준비 완료 후에도 발사/파괴까지 계속 호출한다.
        public static event Action<AirshipYard, RocketSnapshot, float> AssaultTick;
        public static bool AssaultAvailable => ColonyInvasion.FinalAssaultAvailable || AssaultTick != null;
        // 장수 Update보다 먼저 새 소집·복원 장수에도 자동 귀환 억제를 적용한다.
        private void FixedDeploymentHold(bool operating)
        {
            if (CommanderRoster.Instance == null) return;
            foreach (var commander in CommanderRoster.Instance.Commanders)
                if (commander != null) commander.WorkState.holdDeployment = operating && CampaignOutcome.Survivor(commander)
                    && !commander.IsAwayFromHome;
        }
        private void Awake() => Instance = this;
        private void Update() => FixedDeploymentHold(Operating);
        private void LateUpdate() => FixedDeploymentHold(Operating);
        private void Start()
        {
            if (state.initialCommanders == 0 && GameSession.Exists) state.initialCommanders = GameSession.Instance.Options.startingCount;
            if (!state.hasHome) SetHome(AntColony.World.WorldMapManager.Instance != null
                ? AntColony.World.WorldMapManager.Instance.HomePosition : transform.position);
        }
        private void OnDestroy()
        {
            if (Instance == this) { Instance = null; AssaultTick = null; }
        }
        public void SetHome(Vector3 point) { state.home = point; state.hasHome = true; }
        public void RocketDestroyed()
        {
            state.crisisResumeAt = GameCalendar.GameSeconds + 3 * GameCalendar.SecondsPerDay;
            state.assaultStarted = false;
            FixedDeploymentHold(false);
        }
        public static void TickAssault(AirshipYard yard, RocketSnapshot snapshot, float seconds)
        {
            if (yard == null || !yard.Operating || yard.IsDead || !RocketSnapshot.Present(snapshot)
                || !snapshot.Valid || !(seconds > 0) || float.IsInfinity(seconds)) return;
            Instance?.FixedDeploymentHold(true);
            Instance?.GenerateAssault(snapshot, seconds);
            AssaultTick?.Invoke(yard, JsonUtility.FromJson<RocketSnapshot>(JsonUtility.ToJson(snapshot)), seconds);
        }
        private void GenerateAssault(RocketSnapshot snapshot, float seconds)
        {
            if (!state.assaultStarted || state.assaultAt != snapshot.at)
            {
                state.assaultStarted = true; state.assaultAt = snapshot.at;
                state.assaultWave = 0; state.assaultRemaining = ColonyInvasion.FinalFirstDelay;
            }
            state.assaultRemaining -= seconds;
            if (state.assaultRemaining > 0) return;
            if (ColonyInvasion.SpawnFinalWave(snapshot, state.assaultWave, out var nextDelay)
                && state.assaultWave < int.MaxValue) state.assaultWave++;
            state.assaultRemaining = nextDelay;
        }
        public void RecordEnding(AirshipYard yard) => state.ending = CampaignOutcome.Capture(yard.Passengers);
        public State Capture() => JsonUtility.FromJson<State>(JsonUtility.ToJson(state));
        public void Restore(State value)
        {
            if (!Validate(value)) throw new FormatException("Invalid rocket campaign state.");
            state = value == null ? new State() : JsonUtility.FromJson<State>(JsonUtility.ToJson(value));
            if (!state.hasHome) SetHome(AntColony.World.WorldMapManager.Instance?.HomePosition ?? transform.position);
            if (state.initialCommanders == 0 && GameSession.Exists) state.initialCommanders = GameSession.Instance.Options.startingCount;
        }
        public static bool Validate(State value) => value == null || RocketSnapshot.Finite(value.crisisResumeAt)
            && value.initialCommanders >= 0
            && value.assaultWave >= 0 && RocketSnapshot.Finite(value.assaultAt) && RocketSnapshot.Finite(value.assaultRemaining)
            && value.economy != null && value.economy.Valid() && CampaignOutcome.Validate(value.ending)
            && !float.IsNaN(value.home.x) && !float.IsInfinity(value.home.x)
            && !float.IsNaN(value.home.y) && !float.IsInfinity(value.home.y)
            && !float.IsNaN(value.home.z) && !float.IsInfinity(value.home.z);
    }
}

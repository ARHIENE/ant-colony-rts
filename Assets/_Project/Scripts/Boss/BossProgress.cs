using System;
using System.Collections.Generic;
using AntColony.Core;

namespace AntColony.Boss
{
    public enum BossId { A, B, C, D }

    // CampaignResearch.State 안에 보관한다. 획득·하역 호출은 실제 화물 이동과 같은 트랜잭션에서 한다.
    public sealed class BossProgress
    {
        [Serializable] public sealed class Parcel
        {
            public string id, carrier;
            public int boss, cycle, count;
        }
        [Serializable] public sealed class State
        {
            public bool[] obtained = new bool[4], delivered = new bool[4];
            public bool[] revealed = { true, false, false, false }, defeated = new bool[4];
            public int[] home = new int[4], tools = new int[4], cycle = new int[4];
            public int[] claimed = { -1, -1, -1, -1 };
            public float[] killedAt = new float[4];
            public int[] guardRewards = new int[4];
            public List<Parcel> cargo = new List<Parcel>();
            public List<Parcel> unclaimed = new List<Parcel>();
            public int craftBoss = -1;
            public bool craftRecovery;
            public float craftRemaining;
        }
        public const float RecoverySeconds = 60;
        public const int RecoveryMaterial = 100, RecoveryComponents = 10;
        public static float RespawnSeconds => 12 * GameCalendar.SecondsPerDay;
        private readonly State state;
        public BossProgress(State state) { this.state = state; }
        public bool Obtained(BossId id) => state.obtained[(int)id];
        public bool Delivered(BossId id) => state.delivered[(int)id];
        public bool Revealed(BossId id) => state.revealed[(int)id];
        public bool Defeated(BossId id) => state.defeated[(int)id];
        public int Home(BossId id) => state.home[(int)id];
        public int Tools(BossId id) => state.tools[(int)id];
        public int Cycle(BossId id) => state.cycle[(int)id];
        public bool TryConsumeRocketCore()
        {
            if (!Delivered(BossId.D) || Home(BossId.D) < 1) return false;
            state.home[(int)BossId.D]--;
            return true;
        }
        public float CraftRemaining => state.craftRemaining;
        public IReadOnlyList<Parcel> Cargo => state.cargo;
        public static bool Tradable => false;
        public static bool Recyclable => false;
        public static string Reward(BossId id) => id switch {
            BossId.A => "핵심물 2개 · 내구도 높은 고유 장비", BossId.B => "B 핵심물 · 방열 장비",
            BossId.C => "C 핵심물 · 범위 공격 장비", _ => "로켓 핵심물" };
        public static string Hazard(BossId id) => id switch {
            BossId.A => "강한 직접 공격 / 내구도 높은 장비 권장", BossId.B => "열 위험 / 방열 장비 권장",
            BossId.C => "다수 적 / 범위 공격 장비 권장", _ => "복합 위험 / 방열·범위 공격 장비 권장" };
        public bool Recipe(BossId id) => id switch {
            BossId.A => false, BossId.B or BossId.C => Delivered(BossId.A),
            BossId.D => Delivered(BossId.B) && Delivered(BossId.C), _ => false };
        public string DetectionBlock(BossId id)
        {
            if (Revealed(id)) return "위치 영구 공개됨";
            if (!Recipe(id)) return id == BossId.D ? "B·C 핵심물 본거지 반입 필요" : "A 핵심물 본거지 반입 필요";
            if (id == BossId.D && (Home(BossId.B) < 1 || Home(BossId.C) < 1)) return "B·C 고유 핵심물 각 1개 필요";
            if ((id == BossId.B || id == BossId.C) && Home(BossId.A) < 1) return "A 핵심물 1개 필요";
            return "";
        }
        public bool RecordKill(BossId id, float now)
        {
            var i = (int)id;
            if (!Revealed(id) || state.defeated[i] || !Finite(now) || now < 0) return false;
            state.defeated[i] = true; state.killedAt[i] = now;
            state.unclaimed.Add(new Parcel { id = Guid.NewGuid().ToString("N"), carrier = "ground", boss = i,
                cycle = state.cycle[i], count = id == BossId.A ? 2 : 1 });
            return true;
        }
        public bool TryRespawn(BossId id, float now, bool alliesPresent, bool transportPresent, bool inCombat)
        {
            var i = (int)id;
            if (!Finite(now) || !state.defeated[i] || now - state.killedAt[i] < RespawnSeconds
                || alliesPresent || transportPresent || inCombat) return false;
            state.defeated[i] = false; state.cycle[i]++;
            EnsureGuardRewards(); state.guardRewards[i] = 0;
            return true;
        }
        public const int GuardRewardsPerCycle = 8;
        private void EnsureGuardRewards() { if (state.guardRewards == null || state.guardRewards.Length == 0) state.guardRewards = new int[4]; }
        public int ClaimGuardXp(BossId id, int cycle)
            => ClaimGuardExperience(id, cycle, AntColony.Units.CommanderProgression.MonsterKillXp);
        public int ClaimGuardExperience(BossId id, int cycle, int requested)
        {
            EnsureGuardRewards();
            int i = (int)id;
            if (requested <= 0 || cycle != state.cycle[i]) return 0;
            int awarded = Math.Min(requested, GuardRewardsPerCycle * AntColony.Units.CommanderProgression.MonsterKillXp - state.guardRewards[i]);
            state.guardRewards[i] += awarded; return awarded;
        }
        public bool TryAcquire(BossId id, string carrier, out Parcel parcel)
        {
            parcel = null; var i = (int)id;
            if (string.IsNullOrWhiteSpace(carrier)) return false;
            parcel = state.unclaimed.Find(p => p.boss == i);
            if (parcel == null) return false;
            state.unclaimed.Remove(parcel); parcel.carrier = carrier;
            state.cargo.Add(parcel); state.obtained[i] = true; state.claimed[i] = Math.Max(state.claimed[i], parcel.cycle);
            return true;
        }
        // 실제 하역 또는 본거지 전달 직전에 호출. 영구 기록이 소비·후속 해금보다 먼저다.
        public bool TryDeliver(string parcelId)
        {
            var parcel = state.cargo.Find(p => p.id == parcelId);
            if (parcel == null) return false;
            state.delivered[parcel.boss] = true;
            state.home[parcel.boss] += parcel.count;
            state.cargo.Remove(parcel);
            return true;
        }
        public bool LoseCargo(string parcelId)
        {
            var parcel = state.cargo.Find(p => p.id == parcelId);
            return parcel != null && state.cargo.Remove(parcel);
        }
        // 복구는 핵심물만 준다. 장비·희귀 보상은 회차별 보상 회수 경로에서만 지급한다.
        public bool TryStartCraft(BossId id, bool recovery, ResourceManager resources)
        {
            if (resources == null || state.craftBoss >= 0) return false;
            if (recovery ? !Obtained(id) : DetectionBlock(id) != "" || Tools(id) > 0) return false;
            var workshop = AntColony.Core.SceneObjects.All<AntColony.Buildings.Workshop>();
            foreach (var bench in workshop)
            {
                if (!bench.EnqueueBoss(id, recovery)) continue;
                state.craftBoss = (int)id; state.craftRecovery = recovery; state.craftRemaining = RecoverySeconds;
                break;
            }
            if (state.craftBoss < 0) return false;
            if (!recovery)
            {
                if (id == BossId.D) { state.home[1]--; state.home[2]--; }
                else state.home[0]--;
            }
            return true;
        }
        public bool ResumeLegacyCraft(AntColony.Buildings.Workshop bench)
        {
            return state.craftBoss >= 0 && !AntColony.Buildings.Workshop.HasBossJob && bench != null
                && bench.EnqueueBoss((BossId)state.craftBoss, state.craftRecovery, state.craftRemaining, true);
        }
        public void CancelCraft()
        {
            if (state.craftBoss < 0) return;
            if (!state.craftRecovery)
            {
                if (state.craftBoss == 3) { state.home[1]++; state.home[2]++; }
                else state.home[0]++;
            }
            state.craftBoss = -1; state.craftRemaining = 0;
        }
        public static AntColony.Data.ResourceType EraMaterial(BossId id) => id switch {
            BossId.A => AntColony.Data.ResourceType.Plank, BossId.B => AntColony.Data.ResourceType.Iron,
            BossId.C => AntColony.Data.ResourceType.Steel, _ => AntColony.Data.ResourceType.Steel };
        public static Dictionary<AntColony.Data.ResourceType, int> CraftCost(BossId id, bool recovery) => recovery
            ? new Dictionary<AntColony.Data.ResourceType, int> { [EraMaterial(id)] = RecoveryMaterial, [AntColony.Data.ResourceType.Component] = RecoveryComponents }
            : id == BossId.D ? new Dictionary<AntColony.Data.ResourceType, int>()
            : new Dictionary<AntColony.Data.ResourceType, int> { [AntColony.Data.ResourceType.Wood] = 5, [AntColony.Data.ResourceType.Stone] = 5 };
        public void TickCraft(float seconds)
        {
            if (!Finite(seconds) || seconds <= 0 || state.craftBoss < 0) return;
            SetCraftWork(RecoverySeconds - state.craftRemaining + seconds);
        }
        internal void SetCraftWork(float work)
        {
            if (!Finite(work) || work < 0 || state.craftBoss < 0) return;
            state.craftRemaining = Math.Max(0, RecoverySeconds - work);
            if (state.craftRemaining > 0) return;
            if (state.craftRecovery) state.home[state.craftBoss]++;
            else if (!state.revealed[state.craftBoss]) state.tools[state.craftBoss]++;
            state.craftBoss = -1;
        }
        public bool UseDetector(BossId id)
        {
            var i = (int)id;
            if (Revealed(id) || state.tools[i] < 1) return false;
            state.tools[i]--; state.revealed[i] = true;
            return true;
        }
        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        public static bool Validate(State s)
        {
            if (s == null) return true; // 구 저장
            if (s.guardRewards != null && s.guardRewards.Length != 0 && (s.guardRewards.Length != 4 || Array.Exists(s.guardRewards, n => n < 0 || n > GuardRewardsPerCycle * AntColony.Units.CommanderProgression.MonsterKillXp))) return false;
            if (s.obtained?.Length != 4 || s.delivered?.Length != 4 || s.revealed?.Length != 4 || s.defeated?.Length != 4
                || s.home?.Length != 4 || s.tools?.Length != 4 || s.cycle?.Length != 4 || s.claimed?.Length != 4
                || s.killedAt?.Length != 4 || s.cargo == null || s.unclaimed == null || !s.revealed[0]
                || s.craftBoss < -1 || s.craftBoss > 3 || !Finite(s.craftRemaining) || s.craftRemaining < 0
                || s.craftRemaining > RecoverySeconds || (s.craftBoss < 0) != (s.craftRemaining == 0)) return false;
            for (var i = 0; i < 4; i++)
                if (s.home[i] < 0 || s.tools[i] < 0 || s.tools[i] > 1 || s.revealed[i] && s.tools[i] > 0
                    || s.cycle[i] < 0 || s.claimed[i] < -1 || s.claimed[i] > s.cycle[i]
                    || !Finite(s.killedAt[i]) || s.killedAt[i] < 0 || s.delivered[i] && !s.obtained[i]
                    || s.obtained[i] != (s.claimed[i] >= 0)) return false;
            var ids = new HashSet<string>();
            foreach (var p in s.cargo)
                if (p == null || string.IsNullOrWhiteSpace(p.id) || !ids.Add(p.id) || string.IsNullOrWhiteSpace(p.carrier)
                    || p.boss < 0 || p.boss > 3 || p.cycle < 0 || p.cycle > s.cycle[p.boss]
                    || p.count != (p.boss == 0 ? 2 : 1) || !s.obtained[p.boss] || p.cycle > s.claimed[p.boss]) return false;
            var runs = new HashSet<string>();
            foreach (var p in s.cargo) if (!runs.Add(p.boss + ":" + p.cycle)) return false;
            foreach (var p in s.unclaimed)
                if (p == null || string.IsNullOrWhiteSpace(p.id) || !ids.Add(p.id) || p.carrier != "ground"
                    || p.boss < 0 || p.boss > 3 || p.cycle < 0 || p.cycle > s.cycle[p.boss]
                    || p.count != (p.boss == 0 ? 2 : 1) || !runs.Add(p.boss + ":" + p.cycle)) return false;
            return true;
        }
    }
}

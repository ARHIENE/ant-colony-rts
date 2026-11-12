using System;
using AntColony.Core;
using AntColony.UI;
using UnityEngine;

namespace AntColony.World
{
    // 웨이브 수성: 첫 평화 6일 → 웨이브(전멸 또는 대규모 잔당 퇴각) → 평화 4일 반복.
    // 평화가 끝나면 자동 시작, 플레이어가 '지금 시작'으로 앞당길 수 있다. 기존 침입자 생성(LocalIncursions)을 재사용한다.
    // 본거지 장수 수와 무관하게 웨이브를 진행한다. 기존 로켓 공세 억제는 별도 설계 결정까지 유지한다.
    public sealed class WaveDefense : MonoBehaviour
    {
        public const float FirstPeace = 6 * GameCalendar.SecondsPerDay, Peace = 4 * GameCalendar.SecondsPerDay;
        // 마릿수 기준. 개별 유닛(장수·정예 포함)은 앞의 IndividualCap마리까지, 나머지는 HordeSize마리 무리 유닛(2026-11-02 사용자 확정 A+B).
        public const int MaxEnemies = 4000, GroupSize = 20, IndividualCap = 30, HordeSize = 50;
        private const float GroupInterval = .5f, EndGrace = 5f;
        private const int MaxSpawnFailures = 30;
        public const int RetreatMin = 3, RetreatFromTotal = 100;
        public const float RetreatShare = .02f;

        [Serializable] public class State
        {
            public bool saved, inWave;
            public int wave, total, pending, failures;
            public float peaceLeft = FirstPeace, sinceStart;
        }

        public static WaveDefense Instance { get; private set; }
        private State s = new State();
        private float groupTimer;

        public bool InWave => s.inWave;
        public int Wave => s.wave;
        public float PeaceLeft => s.peaceLeft;
        // 이동 중인 문명·반란 공격대는 평화 중 도착해도 다음 웨이브 시작까지 대기한다.
        public static bool AcceptsArrivals => Instance == null || Instance.s.inWave;
        public static bool CanStart => !RocketCampaign.SuppressNewCrises;

        // 2026-11-03 '웨이브 준비 기대치'(사용자 확정, GAME_DIRECTION 6.1)로 맞춘 잠정값. 장수 3·병력 0 실측(WaveBalance1103Checks):
        // 1웨이브 15=장수만으로 막음(체력 19%↓) / 3웨이브 120=방어 없으면 붕괴·탑 2개면 막음 / 5·8웨이브=탑만이면 큰 피해·벽+탑이면 경미.
        // 이후 ×1.15: 10→319, 15→642, 20→1291, 25→2597, 28→3950, 29→4000(상한).
        static readonly int[] EarlyCounts = { 15, 80, 120 };
        public static int Count(int wave) => Mathf.Min(MaxEnemies, DifficultyRuntime.ScaleCount(Mathf.Max(3, Mathf.RoundToInt(StartFactor(wave) * (wave <= 3 ? EarlyCounts[Mathf.Max(1, wave) - 1]
            : EarlyCounts[2] * Mathf.Pow(1.15f, Mathf.Min(wave, 60) - 3))))));
        // 위 실측은 장수 3명 기준. 장수 1명 시작은 1웨이브 15마리에 체력 100→7로 겨우 버텼다(2026-11-03) → 초반은 시작 인원 비례, 6웨이브부터 같은 규모.
        static float StartFactor(int wave) => Mathf.Lerp(Mathf.Clamp(GameSession.Exists ? GameSession.Instance.Options.startingCount : 3, 1, 3) / 3f, 1f, Mathf.Clamp01((wave - 1) / 5f));

        // 목표 우선순위 유형 배분(2026-11-12, 잠정): 무리 10개 중 벽 파괴 1(3웨이브~)·건물 공격 1(4웨이브~)·방어시설 파괴 1(5웨이브~), 나머지는 일반 공격형.
        // 앞의 개별 유닛과 장수는 일반 공격형이다. 초반 방어선을 무의미하게 만들지 않도록 낮게 잡았고 난수를 쓰지 않아 저장·재현에 영향이 없다.
        public static EnemyAim HordeAim(int wave, int hordeIndex)
        {
            var slot = hordeIndex % 10;
            return slot == 2 && wave >= 3 ? EnemyAim.Wall : slot == 5 && wave >= 4 ? EnemyAim.Building
                : slot == 8 && wave >= 5 ? EnemyAim.Defense : EnemyAim.General;
        }

        public int AliveEnemies
        {
            get
            {
                var local = SceneObjects.Any<LocalIncursions>(); var n = 0;
                if (local != null) foreach (var v in local.Visitors) if (v != null && !v.IsDead && !v.Retreating && v.isActiveAndEnabled) n += v.HordeCount;
                return n;
            }
        }
        public int RemainingEnemies => AliveEnemies + (s.inWave ? s.pending : 0);

        public static WaveDefense Ensure()
        {
            if (Instance != null) return Instance;
            var local = SceneObjects.Any<LocalIncursions>();
            return local == null ? null : local.GetComponent<WaveDefense>() ?? local.gameObject.AddComponent<WaveDefense>();
        }

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Update()
        {
            if (!GameSession.Exists || !GameSession.Instance.GameStarted) return;
            var dt = Time.deltaTime;
            if (!s.inWave)
            {
                if (RocketCampaign.SuppressNewCrises) return;
                var before = s.peaceLeft;
                s.peaceLeft = Mathf.Max(0, s.peaceLeft - dt);
                if (before > GameCalendar.SecondsPerDay && s.peaceLeft <= GameCalendar.SecondsPerDay)
                    ToastManager.Show($"웨이브 {s.wave + 1}까지 하루 남았습니다. 방어를 준비하세요.", ToastKind.Warning);
                if (s.peaceLeft <= 0 && CanStart) Begin();
                return;
            }
            s.sinceStart += dt;
            if (s.pending > 0 && (groupTimer -= dt) <= 0) SpawnGroup();
            if (s.sinceStart < EndGrace || s.pending != 0) return;
            var remaining = AliveEnemies;
            if (remaining == 0) End();
            else if (s.total >= RetreatFromTotal && remaining <= Mathf.Max(RetreatMin, Mathf.CeilToInt(s.total * RetreatShare)))
            {
                var local = SceneObjects.Any<LocalIncursions>();
                if (local != null) foreach (var v in local.Visitors)
                    if (v != null && !v.IsDead && v.isActiveAndEnabled) v.BeginRetreat();
                End();
                ToastManager.Show($"남은 적 {remaining}마리가 퇴각합니다");
            }
        }

        public bool StartNow()
        {
            if (s.inWave || !CanStart) return false;
            Begin(); return true;
        }

        private void Begin()
        {
            s.inWave = true; s.wave++; s.total = s.pending = Count(s.wave);
            s.sinceStart = 0; s.failures = 0; groupTimer = 0;
            Storyteller.MarkCrisis(); // 웨이브 직후 사건 감독의 추가 위기를 미룬다.
            ToastManager.Show($"웨이브 {s.wave} 시작 · 적 {s.total}마리 접근", ToastKind.Warning);
            Shout(s.wave == 1 ? "적이 온다! 다들 위치로!" : "또 몰려온다! 전열을 갖춰!");
        }

        private void SpawnGroup()
        {
            groupTimer = GroupInterval;
            var local = SceneObjects.Any<LocalIncursions>();
            var spawned = s.total - s.pending;
            var individuals = Mathf.Min(s.total, IndividualCap) - spawned;
            var count = individuals > 0 ? Mathf.Min(GroupSize, individuals) : Mathf.Min(HordeSize, s.pending);
            var ok = local != null && (individuals > 0 ? local.TrySpawnWaveGroup(count, spawned == 0)
                : local.TrySpawnHorde(count, HordeAim(s.wave, (spawned - Mathf.Min(s.total, IndividualCap)) / HordeSize)));
            if (ok) { s.pending -= count; s.failures = 0; return; }
            // 생성 실패는 처치가 아니다. 남은 병력을 보존하고 느리게 재시도한다.
            // 성공 시 failures를 초기화하므로 경로가 복구되면 기존 묶음 생성으로 돌아간다.
            if (s.failures < MaxSpawnFailures && ++s.failures == MaxSpawnFailures)
            {
                Debug.LogWarning("웨이브 진입 경로를 찾지 못했습니다. 남은 적을 보존하고 재시도합니다.");
                ToastManager.Show("적의 진입 경로를 다시 찾고 있습니다. 방어 성공으로 처리하지 않습니다.", ToastKind.Warning);
            }
            if (s.failures >= MaxSpawnFailures) groupTimer = 5f;
        }

        private void End()
        {
            s.inWave = false; s.peaceLeft = Peace;
            ToastManager.Show($"웨이브 {s.wave} 방어 성공 · 평화 {Peace / GameCalendar.SecondsPerDay:0}일\n"
                + CampaignNextSteps.RecoverySummary(true) + "\n"
                + (ExpeditionRules.Paused ? "복구·연구·방어시설로 다음 웨이브 대비" : "복구·발전과 원정으로 다음 웨이브 대비"), ToastKind.Achievement);
            CampaignHistory.Record("웨이브 방어", $"웨이브 {s.wave}", $"성공 · 적 {s.total}마리");
            Shout("막아냈다… 다친 사람부터 챙기자.");
        }

        // 11/01 장수 서사(말풍선) 연결: 본거지에 있는 장수 한 명이 외친다.
        private static void Shout(string text)
        {
            var roster = CommanderRoster.Instance; if (roster == null) return;
            foreach (var c in roster.Commanders)
                if (c != null && c.IsColonyMember && !c.IsAwayFromHome && !c.IsDead) { SpeechBubbles.Say(c, text, 2); return; }
        }

        public State Capture()
        {
            var copy = JsonUtility.FromJson<State>(JsonUtility.ToJson(s)); copy.saved = true; return copy;
        }

        // 구저장(saved=false)은 웨이브 0회·첫 평화의 남은 시간(최소 60초)으로 이관한다.
        public void Restore(State state)
        {
            if (state == null || !state.saved)
                s = new State { peaceLeft = Mathf.Max(60, FirstPeace - GameCalendar.GameSeconds) };
            else
            {
                s = JsonUtility.FromJson<State>(JsonUtility.ToJson(state));
                s.wave = Mathf.Max(0, s.wave); s.total = Mathf.Clamp(s.total, 0, MaxEnemies);
                s.pending = Mathf.Clamp(s.pending, 0, s.total); s.peaceLeft = Mathf.Clamp(s.peaceLeft, 0, FirstPeace);
            }
            groupTimer = 0;
        }
    }
}

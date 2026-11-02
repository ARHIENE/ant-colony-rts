using System;
using AntColony.Core;
using AntColony.UI;
using UnityEngine;

namespace AntColony.World
{
    // 웨이브 수성(2026-11-02 사용자 확정): 첫 평화 6일 → 웨이브(적 전멸 시 종료) → 평화 4일 반복.
    // 평화가 끝나면 자동 시작, 플레이어가 '지금 시작'으로 앞당길 수 있다. 기존 침입자 생성(LocalIncursions)을 재사용한다.
    // 본거지 장수 0명(G21)·로켓 가동 중에는 웨이브를 시작하지 않는다(원정 중 웨이브·최종 방어 연결은 미정).
    public sealed class WaveDefense : MonoBehaviour
    {
        public const float FirstPeace = 6 * GameCalendar.SecondsPerDay, Peace = 4 * GameCalendar.SecondsPerDay;
        // 마릿수 기준. 개별 유닛(장수·정예 포함)은 앞의 IndividualCap마리까지, 나머지는 HordeSize마리 무리 유닛(2026-11-02 사용자 확정 A+B).
        public const int MaxEnemies = 4000, GroupSize = 20, IndividualCap = 30, HordeSize = 50;
        private const float GroupInterval = .5f, EndGrace = 5f;
        private const int MaxSpawnFailures = 30;

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
        public static bool CanStart => !RocketCampaign.SuppressNewCrises && ExpeditionRules.HomeCommanders > 0;

        // 잠정(2026-11-02, 초반 완만·후반 가파르게·후반 3000 이상): 1→2, 3→4, 5→7, 10→25, 15→93, 20→348, 25→1290, 28→2835, 30→4000(상한).
        public static int Count(int wave) => Mathf.Min(MaxEnemies, DifficultyRuntime.ScaleCount(wave <= 3 ? wave + 1
            : Mathf.RoundToInt(4 * Mathf.Pow(1.3f, Mathf.Min(wave, 60) - 3))));

        public int AliveEnemies
        {
            get
            {
                var local = SceneObjects.Any<LocalIncursions>(); var n = 0;
                if (local != null) foreach (var v in local.Visitors) if (v != null && !v.IsDead && v.isActiveAndEnabled) n += v.HordeCount;
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
            if (s.sinceStart >= EndGrace && s.pending == 0 && AliveEnemies == 0) End();
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
            var ok = local != null && (individuals > 0 ? local.TrySpawnWaveGroup(count, spawned == 0) : local.TrySpawnHorde(count));
            if (ok) { s.pending -= count; s.failures = 0; return; }
            // 진입 경로를 끝내 못 찾으면 남은 병력을 포기한다(전멸 판정이 영원히 막히지 않게).
            if (++s.failures >= MaxSpawnFailures) { s.pending = 0; Debug.LogWarning("웨이브 진입 경로를 찾지 못해 남은 적 생성을 포기했습니다."); }
        }

        private void End()
        {
            s.inWave = false; s.peaceLeft = Peace;
            ToastManager.Show($"웨이브 {s.wave} 방어 성공 · 평화 {Peace / GameCalendar.SecondsPerDay:0}일\n복구·발전과 원정으로 다음 웨이브 대비", ToastKind.Achievement);
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

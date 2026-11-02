using System;
using AntColony.Core;
using AntColony.Data;

namespace AntColony.UI
{
    // 달성 사실은 유지한다. 식량 소비·시설 손실이 이미 끝낸 학습 목표를 되돌리지 않는다.
    public static partial class GoalChain
    {
        [Serializable] public sealed class State { public int completed; public bool initialized; }
        public const int StageCount = 16;
        public const int AllCompleted = (1 << StageCount) - 1;
        private static State state = new State();
        private static CampaignResearch owner;
        private static string firstResearchReward = "과학에서 선택 연구의 해금 항목 확인";
        public static string CurrentText { get; private set; } = "";
        public static string RecentAchievement { get; private set; } = "";
        public static float EmphasisUntil { get; private set; }
        public static int CurrentStage => FirstIncomplete(state.completed);

        public static int FirstIncomplete(int mask)
        {
            for (var i = 0; i < StageCount; i++) if ((mask & (1 << i)) == 0) return i;
            return StageCount;
        }
        public static bool Validate(State value) => value == null || value.completed >= 0
            && (value.completed & ~AllCompleted) == 0 && (value.initialized || value.completed == 0);
        public static State Capture()
        {
            EnsureOwner();
            return new State { completed = state.completed, initialized = state.initialized };
        }
        public static void Restore(State value)
        {
            owner = CampaignResearch.Instance;
            state = value == null ? new State() : new State { completed = value.completed, initialized = value.initialized };
            firstResearchReward = "과학에서 선택 연구의 해금 항목 확인";
            CurrentText = RecentAchievement = ""; EmphasisUntil = 0;
        }
        private static void EnsureOwner()
        {
            if (owner == CampaignResearch.Instance) return;
            Restore(null);
        }

        // 0.5초 간격으로만 호출. 화면의 매 프레임 경로는 캐시한 문자열만 사용한다.
        public static void Refresh(float now)
        {
            EnsureOwner();
            if (owner == null) return;
            if (CurrentStage <= 2)
            {
                if (owner.ActiveResource.HasValue) firstResearchReward = owner.ActiveResource.Value.DisplayName() + " 본격 채굴 가능";
                else if (owner.Active != null) firstResearchReward = owner.Active.Technology switch
                {
                    ScienceTechnology.FungalFarming => "넓은 밭 건설·밭 수확량 증가",
                    ScienceTechnology.HoneydewRanch => "감로 작물 재배 가능",
                    ScienceTechnology.Resin => "흙벽·버섯벽·성벽·문 건설 가능",
                    ScienceTechnology.Traps => "끈끈이·가시 함정 건설 가능",
                    ScienceTechnology.Herbs => "경상 회복·중상 치료 속도 상승",
                    ScienceTechnology.Sanitation => "곰팡이 전파 감소·치료 시간 단축",
                    ScienceTechnology.Fishing => "낚시로 식량 확보 가능",
                    _ => owner.Active.Name + " 활용 · 과학에서 해금 항목 확인"
                };
            }
            var before = state.completed;
            Observe();
            var changed = state.completed & ~before;
            CurrentText = WaveLine() + Describe(CurrentStage);
            if (state.initialized && changed != 0)
            {
                var last = 0;
                for (var i = 0; i < StageCount; i++) if ((changed & (1 << i)) != 0) last = i;
                RecentAchievement = Title(last) + " 달성";
                EmphasisUntil = now + 4f;
                ToastManager.Show(RecentAchievement + "\n" + Reward(last) + "\n다음: "
                    + (CurrentStage == StageCount ? "탈출 기록 확인" : Title(CurrentStage)), ToastKind.Achievement);
            }
            state.initialized = true; // 구저장은 현재 사실로 조용히 이관한다. 불러올 때 완료 알림을 재생하지 않는다.
        }
        private static void Complete(int stage, bool done) { if (done) state.completed |= 1 << stage; }

        // 웨이브 수성(2026-11-02 사용자 승인): 목표 사슬 단계는 바꾸지 않고(저장 비트 유지), 웨이브 중·직전에만 맨 위에 안내를 얹는다.
        private static string WaveLine()
        {
            var wave = AntColony.World.WaveDefense.Instance;
            if (wave == null) return "";
            if (wave.InWave)
                return $"<b>웨이브 {wave.Wave} 방어 · 남은 적 {wave.RemainingEnemies}</b>\n방패 장수를 앞에, 한 대상에 집중, 불리하면 후퇴\n\n";
            if (wave.PeaceLeft > GameCalendar.SecondsPerDay) return "";
            var s = UnityEngine.Mathf.CeilToInt(wave.PeaceLeft);
            return $"<color=#e0a060><b>웨이브 {wave.Wave + 1}까지 {s / 60}:{s % 60:00}</b> · 적 {AntColony.World.WaveDefense.Count(wave.Wave + 1)}마리\n벽·탑·함정과 무기·약품 점검</color>\n\n";
        }
        public static string Title(int stage) => stage switch
        {
            0 => "불시착 생존", 1 => "첫 생활 안정", 2 => "첫 연구 완료", 3 => "첫 시대 승급",
            4 => "첫 원정 출발", 5 => "A 핵심물 반입", 6 => "석유 시대 진입", 7 => "전기 시대 진입",
            8 => "B 핵심물 반입", 9 => "C 핵심물 반입", 10 => "원자 시대 진입", 11 => "미래 시대 진입", 12 => "D 핵심물 반입",
            13 => "로켓 조립", 14 => "최종 방어 완수", 15 => "탑승과 발사", _ => "탈출 완료"
        };
        public static string Reward(int stage) => stage switch
        {
            0 => "잔해의 목재·돌로 첫 시설 준비", 1 => "잠자리·식량을 갖추고 연구에 인력 배정",
            2 => firstResearchReward, 3 => "증기 시대 연구·차량 원정 준비",
            4 => "현장 탐사·표본 회수·전투를 직접 지휘",
            5 => "B·C 탐지의 핵심물 조건 충족", 6 => "고급 작물·치수 공사 연구 가능",
            7 => "항공 수송·냉장 물류 연구 가능", 8 => "D 탐지에 필요한 B 핵심물 확보",
            9 => "B·C 반입 시 D 탐지 준비 가능", 10 => "보온·부위 재생 연구 가능",
            11 => "로켓 연구와 탈출 준비 가능", 12 => "추진기관 연구의 핵심물 조건 충족",
            13 => "가동·방어 준비를 직접 시작", 14 => "탑승·발사를 직접 결정", 15 => "구출·잔류 기록 확인", _ => "구출·잔류 기록 확인"
        };
    }
}

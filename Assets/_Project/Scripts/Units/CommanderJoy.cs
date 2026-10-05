using System;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    // 오락 욕구(2026-09-28): 낮아지면 기분이 떨어지고, 일 사이에 오락 시설에 가서 채운다.
    [Serializable] public class CommanderJoyState
    {
        public float joy = 100, playSeconds, retrySeconds;
        public int kind;
        public float[] boredom = new float[RecreationSpot.KindCount]; // 종류별 질림 0~1
        public bool Valid => Finite(joy) && joy >= 0 && joy <= 100 && Finite(playSeconds) && playSeconds >= 0 && Finite(retrySeconds) && retrySeconds >= 0
            && kind >= 0 && kind < RecreationSpot.KindCount && boredom != null && boredom.Length == RecreationSpot.KindCount && boredom.All(b => Finite(b) && b >= 0 && b <= 1);
        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }

    public partial class CommanderAnt
    {
        private CommanderJoyState JoyState => PersonalState.joy;
        public float Joy => JoyState.joy;
        public bool IsPlaying => JoyState.playSeconds > 0;
        public RecreationSpot PlaySpot { get; private set; }

        // TickDuty에서 식사 다음에 부른다. true면 이번 틱은 오락으로 끝낸다.
        private bool TickJoy(float seconds)
        {
            var j = JoyState;
            j.joy = Mathf.Max(0, j.joy - GameBalance.JoyPerSecond * seconds);
            for (var i = 0; i < j.boredom.Length; i++) j.boredom[i] = Mathf.Max(0, j.boredom[i] - GameBalance.BoredomRecoverPerSecond * seconds);
            RefreshJoyMood();
            if (IsDeployed || IsAwayFromHome || IsEmbarked || IsCaptive) { LeavePlay(); return false; }
            if (j.playSeconds > 0)
            {
                j.playSeconds = Mathf.Max(0, j.playSeconds - seconds);
                if (j.playSeconds == 0) FinishPlay();
                return true;
            }
            if (j.retrySeconds > 0) { j.retrySeconds = Mathf.Max(0, j.retrySeconds - seconds); return false; }
            if (j.joy > GameBalance.PlayBelowJoy || IsCarrying || LabUpgradeBusy) return false;
            // ponytail: 매 틱 가장 가까운 빈 시설을 찾는다. 시설이 많아지면 목표를 캐시한다.
            var spot = RecreationSpot.All.Where(s => s != null && s.HasSeat).OrderBy(s => (s.Position - Position).sqrMagnitude).FirstOrDefault();
            if (spot == null) return false; // 시설이 없으면 계속 일한다(기분 페널티만).
            if (IsWorking || ServiceTarget != null || HuntTarget != null || ScienceAssignment != null || CraftingWorkshop != null) CommandStop();
            if (!CanReceiveOrders) return false;
            if ((spot.Position - Position).sqrMagnitude > 49)
            {
                var moving = IsFlying ? !HasReachedDestination() : Agent.pathPending || Agent.hasPath;
                if (!moving && UnityEngine.AI.NavMesh.SamplePosition(spot.Position, out var hit, 7, UnityEngine.AI.NavMesh.AllAreas) && CanReach(hit.position))
                { base.CommandMove(hit.position); return true; }
                if (moving) return true;
                j.retrySeconds = GameBalance.StarveRetrySeconds; return false; // 닿지 못하면 잠시 뒤 다시.
            }
            if (!spot.Join(this)) return false;
            PlaySpot = spot; j.kind = spot.KindIndex; j.playSeconds = GameBalance.PlaySeconds;
            return true;
        }

        private void FinishPlay()
        {
            var j = JoyState; var spot = PlaySpot;
            var gain = GameBalance.PlayJoy * (1 - j.boredom[j.kind]) * (spot != null && spot.NearDecoration ? 1 + GameBalance.DecorationPlayBonus : 1);
            j.joy = Mathf.Min(100, j.joy + gain);
            j.boredom[j.kind] = Mathf.Min(1, j.boredom[j.kind] + GameBalance.BoredomPerPlay);
            if (j.kind == 2) GainExperience(CommanderActivity.Research, GameBalance.BookshelfResearchXp); // 책장: 연구 경험치 조금
            if (j.kind == 3) PersonalState.hygiene.hygiene = Mathf.Min(100, PersonalState.hygiene.hygiene + GameBalance.BathtubHygiene); // 목욕통: 위생
            if (spot != null && spot.IsGambling && spot.Partner(this) is CommanderAnt other)
            {
                var won = UnityEngine.Random.value < .5f;
                (won ? this : other).personalState.AddMood("도박 승리", GameBalance.GambleWinMood, 180);
                (won ? other : this).personalState.AddMood("도박 패배", GameBalance.GambleLoseMood, 180);
                if (UnityEngine.Random.value < GameBalance.GambleQuarrelChance) ChangeRelation(other, GameBalance.GambleQuarrel);
            }
            // 질려서 다 못 채웠으면 한동안 일하다 다시 온다(같은 곳에서 계속 놀지 않게).
            if (j.joy <= GameBalance.PlayBelowJoy) j.retrySeconds = GameBalance.StarveRetrySeconds * 2;
            LeavePlay(); RefreshJoyMood();
        }

        private void LeavePlay()
        {
            PlaySpot?.Leave(this); PlaySpot = null;
            JoyState.playSeconds = 0;
        }

        private void RefreshJoyMood()
        {
            var j = JoyState;
            var low = j.joy <= 10 ? GameBalance.VeryLowJoyMood : j.joy <= GameBalance.PlayBelowJoy ? GameBalance.LowJoyMood : 0;
            if (low != 0) personalState.AddMood("오락 부족", low, 5); else personalState.moodFactors.RemoveAll(f => f.reason == "오락 부족");
            var kinds = j.boredom.Count(b => b > 0);
            if (kinds >= 2) personalState.AddMood("다양한 오락", kinds * GameBalance.VarietyMoodPerKind, 5); else personalState.moodFactors.RemoveAll(f => f.reason == "다양한 오락");
        }
    }
}

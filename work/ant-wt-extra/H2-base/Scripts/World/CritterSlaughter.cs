using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    // 수동 도축(2026-10-11 기획 반영): 자동 도축·자동 대상 선정·도축 금지 설정은 폐기.
    // 유저가 개체를 직접 지정(이름·마릿수 확인 후)하면 사육 장수가 현장에서 도축한다. 야생은 먼저 포획(결박) 후 도축.
    // 대기·포획 중에는 취소 가능(포획 후 취소하면 결박 유지), 실제 도축 시작 후에는 취소 불가.
    // 강제 중단(전투 등)이면 부분 피해만 입고 생산물 없이 살아남으며 결박 시간이 다시 흐르고, 재작업은 처음부터.
    public sealed partial class Critter
    {
        public bool SlaughterOrdered => s.slaughterOrder;
        public bool SlaughterStarted => s.slaughterStarted;
        // 요청할 수 없는 이유(없으면 null).
        public string SlaughterBlock => Monster == null || Monster.IsDead ? "죽음" : Clinic != null ? "치료 시설 안" : Carrier != null ? "운반 중" : s.slaughterOrder ? "이미 요청됨" : null;
        public bool RequestSlaughter() { if (SlaughterBlock != null) return false; s.slaughterOrder = true; return true; }
        public bool CancelSlaughter()
        {
            if (!s.slaughterOrder || s.slaughterStarted) return false;
            s.slaughterOrder = false;
            if (Handler != null && Handler.AnimalJob == AnimalTask.Slaughter) Handler.CommandStop();
            return true;
        }
        internal void BeginSlaughter() { s.slaughterStarted = true; CalledTo = null; Halt(); }
        internal void FinishSlaughter() { s.slaughterStarted = false; s.slaughterOrder = false; Die("도축"); }
        internal void InterruptSlaughter()
        {
            if (!s.slaughterStarted) return;
            s.slaughterStarted = false;
            if (Monster != null && !Monster.IsDead) Monster.Wound(Mathf.Min(Info.health * GameBalance.SlaughterInterruptDamage, Monster.CurrentHealth - 1)); // 부분 피해, 살아남는다
        }
        // 치료 시설에 들어가면 시작 전 도축 요청은 취소된다(시설 안 동물은 도축 불가).
        private void DropSlaughterOnAdmission() { if (!s.slaughterStarted) s.slaughterOrder = false; }
    }
}

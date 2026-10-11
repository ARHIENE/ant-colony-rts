using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    // 도축대(2026-10-11 기획 반영): 사체별로 등록 순서대로 해체한다(부패·거리·우선 지정으로 순서를 바꾸지 않음).
    // - 산출은 그 사체의 종·성장 단계·이미 먹힌 양(남은 고기·갑각)으로 정해지고, 요리 능력은 속도에만 영향.
    // - 완료하면 생산물을 도축대 옆 바닥에 한 번에 둔다(운반 장수가 회수, 창고 포화는 해체를 막지 않음).
    // - 허용 종: 해제하면 대기 사체 중 그 종은 옆 바닥에 사체로 내려놓는다(사용자 확정). 이미 시작한 해체는 끝까지.
    // - 일시정지: 진행도·사체 보존, 새 운반·해체는 시작하지 않는다. 이미 운반 중인 사체는 받아 둔다. 재개하면 이어서.
    // - 부패: 사체별 남은 부패 시간(Corpse.remaining)을 그대로 옮겨 대기·해체·일시정지 중에도 계속 줄인다. 다 썩으면 해체 불가로 대기열에서 빠지고(진행 중이면 작업 중단), 바닥에 내려놓을 때도 남은 시간을 유지한다.
    public sealed partial class RanchFacility
    {
        [Serializable] public class CarcassEntry { public int species = -1; public float meat; public int chitin; public float decay = Corpse.Lifetime; } // species -1 = 옛 저장(종 미상), decay = 남은 부패 시간(초)

        public IReadOnlyList<CarcassEntry> Queue => s.queue;
        public int Carcasses => s.queue.Count;
        public float ButcherProgress => s.butcher;
        public bool Paused { get => s.paused; set => s.paused = value; }
        public bool AllowsSpecies(Species sp) => AllowsSpecies((int)sp);
        private bool AllowsSpecies(int sp) => sp < 0 || (s.allowMask & (1 << sp)) != 0;

        public void ToggleSpecies(Species sp)
        {
            s.allowMask ^= 1 << (int)sp;
            if (AllowsSpecies(sp)) return;
            for (var i = s.queue.Count - 1; i >= 0; i--)
            {
                if (s.queue[i].species != (int)sp || i == 0 && s.butcher > 0) continue;
                DropEntry(s.queue[i]); s.queue.RemoveAt(i);
            }
        }

        // 새 운반 목적지: 사용 가능·일시정지 아님·그 종 허용. 가까운 도축대부터.
        public bool AcceptsHaul(Species sp) => IsButcher && Usable && !s.paused && AllowsSpecies(sp);
        public static RanchFacility ButcherFor(Species sp, Vector3 from)
            => All.Where(f => f.AcceptsHaul(sp)).OrderBy(f => (f.Position - from).sqrMagnitude).FirstOrDefault();

        internal void AddCarcass(CritterCarcass k)
        {
            var e = new CarcassEntry { species = (int)k.Species, meat = k.Meat, chitin = k.Chitin, decay = k.Corpse != null ? k.Corpse.Data.remaining : Corpse.Lifetime };
            if (AllowsSpecies(k.Species)) s.queue.Add(e); else DropEntry(e);
        }

        private void Update() { if (!AntColony.Save.SaveSystem.Busy) TickDecay(Time.deltaTime); }
        // 부패는 대기·해체·일시정지와 무관하게 흐른다. 다 썩은 사체는 해체할 수 없어 빠진다(맨 앞이면 진행도도 초기화).
        public void TickDecay(float seconds)
        {
            if (!IsButcher || !(seconds > 0) || float.IsInfinity(seconds)) return;
            for (var i = s.queue.Count - 1; i >= 0; i--)
            {
                if ((s.queue[i].decay -= seconds) > 0) continue;
                if (i == 0) s.butcher = 0;
                s.queue.RemoveAt(i);
            }
        }
        public bool NeedsButcher => IsButcher && Usable && !s.paused && s.queue.Count > 0;
        internal bool Butcher(CommanderAnt c, float seconds)
        {
            if (!NeedsButcher) return false;
            s.butcher += seconds * c.WorkRate(CommanderActivity.Cooking); c.GainExperience(CommanderActivity.Cooking, seconds);
            if (s.butcher < GameBalance.ButcherSeconds) return true;
            var e = s.queue[0]; s.queue.RemoveAt(0); s.butcher = 0;
            var at = Position + Vector3.forward * 1.5f;
            var meat = Mathf.FloorToInt(e.meat); if (meat > 0) DiplomacyManager.DropFloor(ResourceType.Food, meat, at);
            if (e.chitin > 0) DiplomacyManager.DropFloor(ResourceType.Chitin, e.chitin, at);
            return s.queue.Count > 0;
        }

        private void DropEntry(CarcassEntry e) => CritterCarcass.Spawn(e.species, e.meat, e.chitin, Position + Vector3.right * 1.5f, e.decay);
        private void DropQueue() { foreach (var e in s.queue) DropEntry(e); s.queue.Clear(); s.butcher = 0; }

        // 옛 저장(사체 수·고기·갑각 합산): 사체 수만큼 똑같이 나눠 종 미상 사체로 옮긴다.
        private void RestoreQueue()
        {
            s.queue ??= new List<CarcassEntry>();
            s.queue.RemoveAll(e => e == null || !(e.meat >= 0) || float.IsInfinity(e.meat) || !(e.decay > 0) || float.IsInfinity(e.decay)); // 저장된 남은 부패 시간 유지(옛 저장은 기본 수명)
            if (s.carcasses > 0 && s.queue.Count == 0)
                for (var i = 0; i < s.carcasses; i++) s.queue.Add(new CarcassEntry { meat = s.meat / s.carcasses, chitin = Mathf.FloorToInt(s.chitin / s.carcasses) });
            s.carcasses = 0; s.meat = s.chitin = 0;
            if (s.queue.Count == 0) s.butcher = 0;
        }
    }
}

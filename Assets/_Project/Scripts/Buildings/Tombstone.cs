using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    // 묘비(2026-10-13): 한 개에 한 구. 고인 이름을 먼저 등록할 수 있고(시신을 못 찾은 경우), 시신을 찾으면 같은 묘비에 안치한다.
    // 안치된 시신은 부패하지 않는다. 시신이 있는 묘비는 철거할 수 없다(먼저 꺼내거나 이장).
    public sealed class Tombstone : BuildingBase
    {
        [Serializable] public sealed class State
        {
            public string deceasedId = "", name = "", cause = ""; public float date;
            public bool body, burialOrdered; public float bodyRemaining; public bool remains;
        }
        public static readonly List<Tombstone> All = new List<Tombstone>();
        public State Data2 { get; private set; } = new State();
        public void RestoreState(State s) => Data2 = s ?? new State();
        protected override void OnEnable() { base.OnEnable(); if (!All.Contains(this)) All.Add(this); }
        protected override void OnDisable() { All.Remove(this); base.OnDisable(); }

        public static Tombstone Of(string deceasedId) => All.FirstOrDefault(t => t.Data2.deceasedId == deceasedId);
        public static Corpse BodyOf(string deceasedId) => Corpse.All.FirstOrDefault(c => c.Data.kind == CorpseKind.ColonyCommander && c.Data.commanderId == deceasedId);
        public bool Empty => Data2.deceasedId == "";

        // 고인 등록: 같은 고인의 묘비를 여러 개 만들 수 없다(효과 중첩 금지).
        public string RegisterBlock(Memorial.Record r) => r == null ? "기록 없음" : !Empty ? "이미 고인이 등록된 묘비" : Of(r.id) != null ? "이미 다른 묘비에 등록됨" : "";
        public bool Register(Memorial.Record r)
        {
            if (RegisterBlock(r) != "") return false;
            Data2 = new State { deceasedId = r.id, name = r.name, cause = r.cause, date = r.date };
            return true;
        }
        // 안치 지시: 시신이 남아 있어야 운반한다(안치할 묘비가 없으면 운반하지 않는다).
        public string BurialBlock => Empty ? "고인 미등록" : Data2.body ? "이미 안치됨" : BodyOf(Data2.deceasedId) == null ? "회수할 시신 없음(나중에 회수하면 안치 가능)" : "";
        public bool OrderBurial(bool on) { if (on && BurialBlock != "") return false; Data2.burialOrdered = on; return true; }
        internal void Inter(Corpse body)
        {
            Data2.body = true; Data2.burialOrdered = false; Data2.bodyRemaining = body.Data.remaining; Data2.remains = body.Data.remaining <= 0;
            body.Bury();
        }
        // 꺼내기·이장: 시신을 옆 바닥에 원래 상태로 내놓는다. 이장할 묘비가 있으면 안치 지시를 건다.
        public Corpse TakeOut(Tombstone moveTo = null)
        {
            if (!Data2.body) return null;
            var corpse = Corpse.Spawn(new Corpse.State { name = Data2.name, kind = CorpseKind.ColonyCommander, commanderId = Data2.deceasedId,
                position = Position + Vector3.right * 1.2f, remaining = Data2.bodyRemaining });
            Data2.body = false;
            if (moveTo != null && moveTo != this)
            {
                var rec = new Memorial.Record { id = Data2.deceasedId, name = Data2.name, cause = Data2.cause, date = Data2.date };
                Data2 = new State();
                moveTo.Register(rec); moveTo.OrderBurial(true);
            }
            return corpse;
        }
        public string RemovalBlock => Data2.body ? "시신이 안치된 묘비는 철거할 수 없습니다(먼저 꺼내거나 이장)" : "";
    }
}

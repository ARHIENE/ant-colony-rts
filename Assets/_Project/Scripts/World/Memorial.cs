using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    // 사망 기록·추모(2026-10-13 확정 기획). 장례식·추모 지시는 없다. 지나가던 장수가 관계에 따라 짧게 추모하고
    // 친한 장수는 여가에 묘비를 찾아간다. 위로는 같은 고인에 대해 중첩·영구 누적하지 않는다.
    public static class Memorial
    {
        [Serializable] public sealed class Record { public string id, name, cause; public float date; }
        [Serializable] public sealed class State { public List<Record> deaths = new List<Record>(); }
        private static State state = new State();
        public static IReadOnlyList<Record> Deaths => state.deaths;
        public static State Capture() => JsonUtility.FromJson<State>(JsonUtility.ToJson(state));
        public static void Restore(State s) => state = s == null ? new State() : JsonUtility.FromJson<State>(JsonUtility.ToJson(s));
        public static Record Find(string id) => state.deaths.Find(r => r.id == id);
        internal static void RecordEnemy(string id, string name)
        {
            if (Find(id) == null) state.deaths.Add(new Record { id = id, name = name, cause = "전투", date = GameCalendar.GameSeconds });
        }
        public static void RecordDeath(CommanderAnt c, string cause)
        {
            if (c == null || Find(c.PersonalState.id) != null) return;
            state.deaths.Add(new Record { id = c.PersonalState.id, name = c.CommanderName, cause = cause, date = GameCalendar.GameSeconds });
        }

        // 같은 고인의 위로는 달력상 하루 한 번만 받는다.
        public const float ComfortSeconds = GameCalendar.SecondsPerDay, ComfortCooldown = GameCalendar.SecondsPerDay, MourningRadius = 4f, SightRadius = 8f;
        public static float ComfortValue(CommanderRelation r) => 3;
        public static float UnburiedMood(CommanderRelation r) => -3;

        // 장수 1명의 주기 판정(CommanderAnt.TickSocial 등에서 1초마다 부른다): 지나가며 추모, 미안치 시신 목격.
        public static void Tick(CommanderAnt c, float dt)
        {
            if (c == null || c.IsDead || !c.IsColonyMember || c.IsEmbarked || c.IsCaptive) return;
            foreach (var r in c.PersonalState.relations) if (r.memorialCooldown > 0) r.memorialCooldown = Mathf.Max(0, r.memorialCooldown - dt);
            foreach (var tomb in Tombstone.All)
            {
                if (tomb.Data2.deceasedId == "" || (tomb.Position - c.Position).sqrMagnitude > MourningRadius * MourningRadius) continue;
                Comfort(c, tomb);
            }
            CommanderRelation worst = null; string seen = null;
            foreach (var body in Corpse.All)
            {
                if (!body.IsCommanderBody || (body.Position - c.Position).sqrMagnitude > SightRadius * SightRadius) continue;
                var r = c.PersonalState.relations.Find(x => x.otherId == body.Data.commanderId);
                if (seen == null || UnburiedMood(r) < UnburiedMood(worst)) { worst = r; seen = body.Data.name; }
            }
            if (seen != null && !c.PersonalState.moodFactors.Any(f => f.reason == "미안치 시신 목격"))
                c.PersonalState.AddMood("미안치 시신 목격", -3, GameCalendar.SecondsPerDay);
        }
        // 같은 고인에 대한 위로는 쿨다운 동안 다시 받지 않는다(지속시간 갱신으로 영구화 방지).
        public static bool Comfort(CommanderAnt c, Tombstone tomb)
        {
            var r = c.PersonalState.relations.Find(x => x.otherId == tomb.Data2.deceasedId);
            var value = ComfortValue(r);
            if (tomb.Empty || r != null && r.lastComfortDay == GameCalendar.Day) return false;
            if (r == null) { r = c.PersonalState.Relation(tomb.Data2.deceasedId); }
            r.memorialCooldown = ComfortCooldown; r.lastComfortDay = GameCalendar.Day;
            c.PersonalState.AddMood("추모: " + tomb.Data2.deceasedId, value, ComfortSeconds);
            return true;
        }
        // 친한 장수의 여가 방문 대상(관계 40 이상, 쿨다운 끝남).
        public static Tombstone VisitTarget(CommanderAnt c) => Tombstone.All.Where(t => t.Data2.deceasedId != "" && t.isActiveAndEnabled
            && c.PersonalState.relations.Any(r => r.otherId == t.Data2.deceasedId && (r.value >= 40 || r.spouse || r.family) && r.lastComfortDay != GameCalendar.Day))
            .OrderBy(t => (t.Position - c.Position).sqrMagnitude).FirstOrDefault();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Units;
using AntColony.Buildings;
using AntColony.World;
using UnityEngine;

namespace AntColony.Core
{
    public enum EndingCategory { Escaped, Staying, Outside, Captive, Dead, Left }
    public static class CampaignOutcome
    {
        public const string CaptureWarning = "마지막 아군이 포로가 되면 즉시 패배합니다. 구출은 남은 아군 필요.";
        public const string HarshWarning = "가혹: 쓰러짐 즉사 30%(잠정). 첫 쓰러짐에서 즉사하면 즉시 패배";
        [Serializable] public sealed class Entry { public string id, name; public EndingCategory category; }
        [Serializable] public sealed class State
        {
            public List<Entry> commanders = new List<Entry>();
            public int citizensEscaped, citizensStaying;
            public List<string> legacy = new List<string>(); // 구저장 null/빈 목록: 당시 서사 스냅샷 없음
        }
        public static bool Survivor(CommanderAnt c) => c != null && !c.IsDead && !c.IsCaptive && !c.IsDeparting && !c.IsHostile;
        public static bool SafeHomecoming(Homecoming h) => h?.commander?.PersonalState != null
            && string.IsNullOrEmpty(h.to) && !h.commander.PersonalState.dead;
        public static bool HasSurvivors => CommanderRoster.Instance != null && CommanderRoster.Instance.Commanders.Any(Survivor)
            || DiplomacyManager.Instance != null && DiplomacyManager.Instance.Data.homecomings.Any(SafeHomecoming);
        public static string SingleHarshWarning => CommanderDeathRuntime.Mode == CommanderDeathMode.Harsh
            && (RocketCampaign.Instance != null ? RocketCampaign.Instance.InitialCommanders : GameSession.Exists ? GameSession.Instance.Options.startingCount : 0) == 1 ? HarshWarning : null;
        public static EndingCategory Classify(CommanderAnt c, bool boarded)
        {
            if (c.IsDead) return EndingCategory.Dead;
            if (c.IsCaptive) return EndingCategory.Captive;
            if (c.IsDeparting || c.IsHostile) return EndingCategory.Left;
            if (boarded && c.IsEmbarked) return EndingCategory.Escaped;
            return c.IsAwayFromHome ? EndingCategory.Outside : EndingCategory.Staying;
        }
        public static State Capture(IReadOnlyList<CommanderAnt> boarded)
        {
            var result = Capture(boarded, null);
            result.legacy = CampaignLegacy.Capture(result);
            return result;
        }
        private static State Capture(IReadOnlyList<CommanderAnt> boarded, CommanderAnt boardingCandidate)
        {
            var result = new State(); var ids = new HashSet<string>();
            var yard = AntColony.Core.SceneObjects.All<AirshipYard>()
                .FirstOrDefault(y => ReferenceEquals(y.Passengers, boarded));
            int civilianPassengers = yard?.CivilianPassengers ?? 0;
            result.citizensEscaped = civilianPassengers;
            void Add(string id, string name, EndingCategory category)
            {
                if (!string.IsNullOrEmpty(id) && ids.Add(id)) result.commanders.Add(new Entry { id = id, name = name, category = category });
            }
            // 석방 후 안전 귀환자는 과거 포로 기록과 중복 없이 외부 이동으로 분류한다.
            if (DiplomacyManager.Instance != null)
                foreach (var h in DiplomacyManager.Instance.Data.homecomings.Where(SafeHomecoming))
                    Add(h.commander.PersonalState.id, h.commander.Name, EndingCategory.Outside);
            if (CommanderRoster.Instance != null)
                foreach (var c in CommanderRoster.Instance.Commanders)
                {
                    var kind = Classify(c, boarded.Contains(c));
                    if (c == boardingCandidate && Survivor(c) && !c.IsAwayFromHome) kind = EndingCategory.Escaped;
                    bool counted = ids.Contains(c.PersonalState.id);
                    Add(c.PersonalState.id, c.CommanderName, kind);
                    if (!counted && kind == EndingCategory.Escaped) result.citizensEscaped += c.TroopCount;
                }
            foreach (var death in Memorial.Deaths) Add(death.id, death.name, EndingCategory.Dead);
            if (CampaignResearch.Instance != null) foreach (var gone in CampaignResearch.Instance.LeftCommanders) Add(gone.id, gone.name, EndingCategory.Left);
            // AntPool.Assigned는 수송·주둔 병력까지 포함한다. 일반 시민 승객만 풀에서 이미 반출됐다.
            int total = ColonyPopulation.Instance?.Total ?? AntPool.Instance?.Total ?? 0;
            result.citizensStaying = Mathf.Max(0, total - (result.citizensEscaped - civilianPassengers));
            return result;
        }
        public static bool Validate(State s) => s == null || s.commanders != null && s.citizensEscaped >= 0 && s.citizensStaying >= 0
            && (s.legacy == null || s.legacy.Count <= 10000 && s.legacy.All(line => line != null && line.Length <= 1000))
            && s.commanders.All(c => c != null && !string.IsNullOrEmpty(c.id) && c.name != null && Enum.IsDefined(typeof(EndingCategory), c.category))
            && s.commanders.Select(c => c.id).Distinct().Count() == s.commanders.Count;
        public static string Preview(IReadOnlyList<CommanderAnt> boarded, CommanderAnt boardingCandidate = null)
        {
            var s = Capture(boarded, boardingCandidate);
            return $"예상 탈출 장수 {s.commanders.Count(c => c.category == EndingCategory.Escaped)}명 / 잔류·외부 장수 {s.commanders.Count(c => c.category == EndingCategory.Staying || c.category == EndingCategory.Outside)}명 · 시민 탈출 {s.citizensEscaped}명 / 잔류 {s.citizensStaying}명";
        }
    }
}

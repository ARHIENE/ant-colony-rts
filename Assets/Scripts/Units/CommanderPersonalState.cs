using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntColony.Units
{
    public enum InjuryPart { Antenna, Mandible, Legs, Thorax, Head, Wings }
    public enum InjurySeverity { Minor, Serious, Permanent }
    public enum MentalBreak { None, Idle, Flee, AttackBuilding, AttackCommander, SelfHarm, Binge }
    [Serializable] public class CommanderInjury { public InjuryPart part; public InjurySeverity severity; public float remaining; }
    [Serializable] public class MoodFactor { public string reason; public float value, remaining; }
    [Serializable] public class CommanderRelation { public string otherId; public float value, nearbySeconds, quarrelSeconds; public bool spouse; }
    [Serializable]
    public class CommanderPersonalState
    {
        public string id = Guid.NewGuid().ToString("N");
        public List<CommanderInjury> injuries = new List<CommanderInjury>();
        public List<MoodFactor> moodFactors = new List<MoodFactor>();
        public List<CommanderRelation> relations = new List<CommanderRelation>();
        public List<EquipmentItem> equipment = new List<EquipmentItem>();
        public float workedSeconds, breakCheck, loyaltyCheck, captiveSeconds, unsupportedSeconds, breakRemaining, rageRemaining, rewardCooldown, lastCombatSeconds, homeSeconds;
        public float craftProgress, researchProgress;
        public int craftLevel, researchLevel;
        public MentalBreak mentalBreak;
        public bool dead, treating;
        public string originFaction = "", departure = "";
        public bool HasTreatableInjury => injuries.Exists(i => i.severity == InjurySeverity.Serious);
        public bool HasSeriousInjury => injuries.Exists(i => i.severity != InjurySeverity.Minor);
        public float Severity(InjuryPart part)
        {
            float value = 0; foreach (var i in injuries) if (i.part == part) value = Mathf.Max(value, i.severity == InjurySeverity.Minor ? .5f : 1f); return value;
        }
        public void AddMood(string reason, float value, float seconds)
        {
            moodFactors.RemoveAll(f => f.reason == reason);
            moodFactors.Add(new MoodFactor { reason = reason, value = value, remaining = seconds });
        }
        public CommanderRelation Relation(string other)
        {
            var result = relations.Find(r => r.otherId == other);
            if (result != null) return result;
            result = new CommanderRelation { otherId = other }; relations.Add(result); return result;
        }
        public void AddInjury(CommanderTraits traits, bool minorOnly = false)
        {
            var part = (InjuryPart)UnityEngine.Random.Range(0, 6);
            var serious = !minorOnly && UnityEngine.Random.value < (traits.Has(CommanderTrait.Robust) ? .1f : traits.Has(CommanderTrait.Frail) ? .5f : .3f);
            var severity = serious ? UnityEngine.Random.value < .1f ? InjurySeverity.Permanent : InjurySeverity.Serious : InjurySeverity.Minor;
            var old = injuries.Find(i => i.part == part);
            if (old != null && old.severity > severity) return;
            if (old != null) injuries.Remove(old);
            injuries.Add(new CommanderInjury { part = part, severity = severity, remaining = severity == InjurySeverity.Minor ? 180 : 240 });
        }
        public void Tick(float dt, CommanderTraits traits)
        {
            foreach (var f in moodFactors) f.remaining -= dt;
            moodFactors.RemoveAll(f => f.remaining <= 0);
            foreach (var injury in injuries)
                if (injury.severity == InjurySeverity.Minor) injury.remaining -= dt * (traits.Has(CommanderTrait.Robust) ? 2 : 1);
                else if (injury.severity == InjurySeverity.Serious && treating) injury.remaining -= dt;
            injuries.RemoveAll(i => i.severity != InjurySeverity.Permanent && i.remaining <= 0);
            rewardCooldown = Mathf.Max(0, rewardCooldown - dt);
            rageRemaining = Mathf.Max(0, rageRemaining - dt);
        }
        public bool Validate(out string error)
        {
            error = "Invalid commander personal state";
            if (string.IsNullOrEmpty(id) || injuries == null || injuries.Count > 6 || moodFactors == null || moodFactors.Count > 64 || relations == null || equipment == null || equipment.Count > 3) return false;
            var parts = new HashSet<InjuryPart>();
            foreach (var i in injuries) if (i == null || !Enum.IsDefined(typeof(InjuryPart), i.part) || !parts.Add(i.part) || !Enum.IsDefined(typeof(InjurySeverity), i.severity) || !Finite(i.remaining) || i.remaining < 0) return false;
            foreach (var f in moodFactors) if (f == null || f.reason == null || !Finite(f.value) || !Finite(f.remaining) || f.remaining < 0) return false;
            foreach (var r in relations) if (r == null || string.IsNullOrEmpty(r.otherId) || !Finite(r.value) || r.value < -100 || r.value > 100 || !Finite(r.nearbySeconds) || !Finite(r.quarrelSeconds)) return false;
            var slots = new HashSet<EquipmentSlot>();
            foreach (var e in equipment) if (e == null || !e.IsValid || !slots.Add(e.slot)) return false;
            foreach (float value in new[] { workedSeconds, breakCheck, loyaltyCheck, captiveSeconds, unsupportedSeconds, breakRemaining, rageRemaining, rewardCooldown, lastCombatSeconds, homeSeconds, craftProgress, researchProgress }) if (!Finite(value) || value < 0) return false;
            if (craftLevel < 0 || craftLevel > 5 || researchLevel < 0 || researchLevel > 5 || !Enum.IsDefined(typeof(MentalBreak), mentalBreak)) return false;
            error = null; return true;
        }
        private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
    }
}

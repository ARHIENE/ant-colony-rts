using System;
using System.Collections.Generic;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    // 효과 공식용 부위 묶음 + 옛 저장(injuries) 형식. 숫자 값 유지(LegacyAntenna = 옛 더듬이, 이관 전용).
    public enum InjuryPart { LegacyAntenna, Arms, Legs, Thorax, Head, Wings }
    public enum InjurySeverity { Minor, Serious, Permanent }
    public enum MentalBreak { None, Idle, Flee, AttackBuilding, AttackCommander, SelfHarm, Binge, Wander }
    [Serializable] public class CommanderInjury { public InjuryPart part; public InjurySeverity severity; public float remaining; public bool regenerating, prognosisPending; }
    [Serializable] public class MoodFactor { public string reason; public float value, remaining; }
    [Serializable] public class CommanderRelation { public string otherId; public float value, nearbySeconds, quarrelSeconds, duelSeconds, memorialCooldown; public bool spouse, family; }
    [Serializable]
    public class CommanderPersonalState
    {
        public CommanderWorkState work = new CommanderWorkState();
        public string id = Guid.NewGuid().ToString("N");
        public List<CommanderInjury> injuries = new List<CommanderInjury>();
        public List<MoodFactor> moodFactors = new List<MoodFactor>();
        public List<CommanderRelation> relations = new List<CommanderRelation>();
        public List<EquipmentItem> equipment = new List<EquipmentItem>();
        public int toolTier; // 0 맨손(옛 저장), 1 나무 세트, 2 돌 세트
        public int appearanceSeed;
        public string ordinaryClothes = "";
        public FoodLots carriedRations = new FoodLots(); // 원식량 단위, 개인 재고는 창고 합계에 포함하지 않는다.
        public GroundCraftState groundCraft = new GroundCraftState();
        public float workedSeconds, breakCheck, lowMoodSeconds, captiveSeconds, unsupportedSeconds, breakRemaining, rageRemaining, rewardCooldown, lastCombatSeconds, homeSeconds;
        public float craftProgress, researchProgress;
        public float beautyExposure; // 생활 공간 미관의 체류 평균(2026-10-10)
        public float ageMonths = -1, lifespanMonths; // Phase 4 나이(개월)
        public int craftLevel, researchLevel;
        public int expectationTier; // 저재산 0 / 정착지급 1 / 산업급 2
        public MentalBreak mentalBreak;
        public bool dead, treating;
        public bool infected;
        public float moldLoss, moldSpread, moldTreatment;
        public string originFaction = "", departure = "";
        public CommanderSocialState social = new CommanderSocialState();
        public CommanderSleepState sleep = new CommanderSleepState();
        public CommanderMealState meal = new CommanderMealState();
        public CommanderJoyState joy = new CommanderJoyState();
        public CommanderHygieneState hygiene = new CommanderHygieneState(); // 2026-10-05, 이전 저장은 기본값 100
        // 2026-10-15 림월드식 부위별 건강(CommanderHealth). injuries·infected는 옛 저장 이관 전용.
        public HealthState health = new HealthState();
        public bool HasTreatableInjury => health.Serious;
        public bool NeedsTreatment => health.Injured || health.Downed || injuries.Count > 0 || infected;
        public bool HasSeriousInjury => health.Serious || injuries.Exists(i => i.severity != InjurySeverity.Minor);
        // 옛 효과 공식(이동·공격·채집·지휘 등)이 쓰는 부위 손상도 0~1. 새 능력치(의식·이동·조작·감지)에서 계산한다.
        public float Severity(InjuryPart part) => part switch
        {
            InjuryPart.Head => 1 - health.Consciousness,
            InjuryPart.Arms => 1 - health.Manipulation,
            InjuryPart.Legs => 1 - health.Moving,
            InjuryPart.Thorax => 1 - health.PartFraction(BodyPart.Thorax),
            _ => 0 // 날개는 장비, 더듬이는 없음(2026-10-18 캐릭터 전환)
        };
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
            var wasSerious = health.Serious;
            MedicalRules.AddRandomWound(health, traits, minorOnly);
            if (!wasSerious && health.Serious) social.seriousInjuries++;
        }
        public void Tick(float dt, CommanderTraits traits, float treatmentRate = 1, float permanentRisk = .1f)
        {
            foreach (var f in moodFactors) f.remaining -= dt;
            moodFactors.RemoveAll(f => f.remaining <= 0);
            rewardCooldown = Mathf.Max(0, rewardCooldown - dt);
            rageRemaining = Mathf.Max(0, rageRemaining - dt);
        }
        public bool Validate(out string error)
        {
            error = "Invalid commander personal state";
            if (expectationTier < 0 || expectationTier > 2 || toolTier < 0 || toolTier > 2 || carriedRations != null && !carriedRations.Valid
                || groundCraft != null && !groundCraft.Valid) return false;
            if (work == null || !work.Valid || string.IsNullOrEmpty(id) || social == null || !social.Validate() || sleep == null || !sleep.Valid || meal == null || !meal.Valid || joy == null || !joy.Valid || hygiene == null || !hygiene.Valid || health == null || health.wounds == null || health.illnesses == null || health.surgeries == null
                || health.wounds.Count > 64 || health.illnesses.Count > 16 || health.surgeries.Count > 8 || !Finite(health.bloodLoss) || health.bloodLoss < 0 || health.bloodLoss > 1
                || health.wounds.Exists(w => w == null || !Enum.IsDefined(typeof(BodyPart), w.part) || !Enum.IsDefined(typeof(WoundKind), w.kind) || !Finite(w.damage) || w.damage < 0 || !Finite(w.age) || !Finite(w.quality))
                || health.illnesses.Exists(i => i == null || !Enum.IsDefined(typeof(IllnessKind), i.kind) || !Finite(i.severity) || !Finite(i.immunity) || !Finite(i.tendLeft))
                || health.bedRest < 0 || health.bedRest > 9 || !Enum.IsDefined(typeof(MedicineLevel), health.medicine)
                || injuries == null || injuries.Count > 6 || moodFactors == null || moodFactors.Count > 64 || relations == null || equipment == null || equipment.Count > 3) return false;
            var parts = new HashSet<InjuryPart>();
            foreach (var i in injuries) if (i == null || !Enum.IsDefined(typeof(InjuryPart), i.part) || !parts.Add(i.part) || !Enum.IsDefined(typeof(InjurySeverity), i.severity) || !Finite(i.remaining) || i.remaining < 0) return false;
            foreach (var f in moodFactors) if (f == null || f.reason == null || !Finite(f.value) || !Finite(f.remaining) || f.remaining < 0) return false;
            foreach (var r in relations) if (r == null || string.IsNullOrEmpty(r.otherId) || !Finite(r.value) || r.value < -100 || r.value > 100 || !Finite(r.nearbySeconds) || r.nearbySeconds < 0 || !Finite(r.quarrelSeconds) || r.quarrelSeconds < 0 || !Finite(r.duelSeconds) || r.duelSeconds < 0) return false;
            var slots = new HashSet<EquipmentSlot>();
            foreach (var e in equipment) if (e == null || !e.IsValid || !slots.Add(e.slot)) return false;
            foreach (float value in new[] { workedSeconds, breakCheck, lowMoodSeconds, captiveSeconds, unsupportedSeconds, breakRemaining, rageRemaining, rewardCooldown, lastCombatSeconds, homeSeconds, craftProgress, researchProgress }) if (!Finite(value) || value < 0) return false;
            if (craftLevel < 0 || craftLevel > 5 || researchLevel < 0 || researchLevel > 5 || !Enum.IsDefined(typeof(MentalBreak), mentalBreak)) return false;
            if (!Finite(moldLoss) || moldLoss < 0 || moldLoss >= 20 || !Finite(moldSpread) || moldSpread < 0 || moldSpread >= 30
                || !Finite(moldTreatment) || moldTreatment < 0 || moldTreatment > 60) return false;
            error = null; return true;
        }
        private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
    }
}

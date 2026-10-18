using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Units
{
    // 장수 부상·질병(2026-10-13 확정 기획, 림월드식). 부위별 상처·출혈·통증·의식·이동, 처치 품질·회복, 질병 심각도·면역.
    // 행동 가능 여부는 경상/중상 요약이 아니라 통증·의식·이동 능력으로 정한다.
    // 2026-10-18 캐릭터 전환: 두 팔·두 다리. v17 이전 저장(더듬이·큰턱·다리 6개)은 BodyMigration이 옮긴다.
    public enum BodyPart { Head, EyeL, EyeR, Thorax, Abdomen, ArmL, ArmR, HandL, HandR, LegL, LegR, FootL, FootR }
    public enum WoundKind { Cut, Stab, Bruise, Burn, Acid, Scar, Missing, Surgical }
    public enum IllnessKind { WoundInfection, Mold }
    public enum MedicineLevel { NoCare, NoMedicine, Basic, Normal, Advanced }

    [Serializable] public class Wound
    {
        public BodyPart part; public WoundKind kind; public float damage, age;
        public bool tended, infectionRolled; public float quality;
        public bool Permanent => kind == WoundKind.Scar || kind == WoundKind.Missing;
        public bool NeedsTend => !tended && (kind != WoundKind.Scar) && (kind != WoundKind.Missing || age < MedicalRules.FreshMissingSeconds);
    }
    [Serializable] public class Illness
    {
        public IllnessKind kind; public BodyPart part; public float severity, immunity, quality, tendLeft;
        public bool NeedsTend => tendLeft <= 0;
    }
    [Serializable] public class HealthState
    {
        public List<Wound> wounds = new List<Wound>();
        public List<Illness> illnesses = new List<Illness>();
        public float bloodLoss;
        public MedicineLevel medicine = MedicineLevel.Normal;
        public bool selfTend = true;
        public int bedRest = 5; // 병상 휴식 우선순위 1~9(높을수록 일보다 휴식), 0 = 쉬지 않음
        public bool migrated;
        public List<SurgeryOrder> surgeries = new List<SurgeryOrder>();

        public float PartFraction(BodyPart p)
        {
            if (Missing(p) || MedicalRules.Parent(p) is BodyPart up && Missing(up)) return 0; // 팔·다리를 잃으면 손·발도 못 쓴다
            return Mathf.Clamp01(1 - wounds.Where(w => w.part == p).Sum(w => w.damage) / MedicalRules.MaxHp(p));
        }
        public bool Missing(BodyPart p) => wounds.Any(w => w.part == p && w.kind == WoundKind.Missing);
        public float Pain => Mathf.Clamp01(wounds.Sum(w => w.damage * MedicalRules.PainFactor(w.kind) * (w.tended ? .6f : 1f)) / MedicalRules.PainScale
            + illnesses.Sum(i => i.severity * .25f));
        public float BleedRate => wounds.Sum(w => MedicalRules.BleedFactor(w) * w.damage * (1 - (w.tended ? w.quality * .5f + .5f : 0))) / MedicalRules.BleedScale;
        public bool Bleeding => BleedRate > 0.0001f;
        public float Consciousness => Mathf.Clamp01(PartFraction(BodyPart.Head) - Mathf.Max(0, Pain - .2f) * .5f - Mathf.Max(0, bloodLoss - .15f) * .9f
            - illnesses.Sum(i => i.severity * .3f));
        public float Moving
        {
            get
            {
                var legs = (Limb(BodyPart.LegL, BodyPart.FootL) + Limb(BodyPart.LegR, BodyPart.FootR)) / 2;
                return Mathf.Clamp01(legs * Mathf.Min(1, Consciousness * 1.25f));
            }
        }
        public float Manipulation => Mathf.Clamp01((Limb(BodyPart.ArmL, BodyPart.HandL) + Limb(BodyPart.ArmR, BodyPart.HandR)) / 2 * Mathf.Min(1, Consciousness * 1.25f));
        public float Sight => (PartFraction(BodyPart.EyeL) + PartFraction(BodyPart.EyeR)) / 2;
        // 팔·다리 한쪽 기능: 큰 부위가 주, 손·발이 보조.
        private float Limb(BodyPart limb, BodyPart end) => PartFraction(limb) * (.6f + .4f * PartFraction(end));
        // 쓰러짐: 통증 쇼크·의식 저하·이동 불가.
        public bool Downed => Pain >= .8f || Consciousness <= .3f || Moving <= .2f;
        public bool Fatal => bloodLoss >= 1 || PartFraction(BodyPart.Head) <= 0 || PartFraction(BodyPart.Thorax) <= 0 || PartFraction(BodyPart.Abdomen) <= 0
            || illnesses.Any(i => i.severity >= 1);
        // 출혈 지속 시 예상 사망까지 남은 초. 출혈이 없으면 무한.
        public float SecondsToBleedOut => Bleeding ? (1 - bloodLoss) / BleedRate : float.PositiveInfinity;
        public bool LifeThreatening => SecondsToBleedOut < MedicalRules.LifeThreatSeconds || illnesses.Any(i => i.severity > .7f) || Downed && Bleeding;
        public bool Serious => LifeThreatening || Pain > .4f || Consciousness < .6f || Moving < .6f || illnesses.Any(i => i.severity > .4f) || wounds.Any(w => w.NeedsTend && w.damage >= MedicalRules.MaxHp(w.part) * .5f);
        public bool Injured => wounds.Any(w => !w.Permanent || w.kind == WoundKind.Missing) || illnesses.Count > 0 || bloodLoss > .05f;
        public bool NeedsTend => wounds.Any(w => w.NeedsTend) || illnesses.Any(i => i.NeedsTend);
        public string Summary => Downed ? "쓰러짐" : LifeThreatening ? "위독" : Serious ? "중상" : Injured ? "경상" : "건강";
    }

    // ponytail: 모든 수치는 기획 미정 잠정값(2026-10-15). 확정되면 여기만 고친다.
    public static class MedicalRules
    {
        public const float PainScale = 40f, BleedScale = 900f, BloodRegen = .0015f, LifeThreatSeconds = 90f, FreshMissingSeconds = 60f;
        public const float HealTended = .02f, HealUntended = .005f, ScarChance = .25f, InfectionChance = .3f, InfectionAfter = 45f;
        public const float TendSeconds = 6f, IllnessTendInterval = 90f, SelfTendFactor = .7f;
        public static Func<float> Roll = () => UnityEngine.Random.value; // 검사에서 결과를 결정적으로 재현하려고 바꿀 수 있다
        public static float MaxHp(BodyPart p) => p switch
        {
            BodyPart.Head => 10, BodyPart.EyeL or BodyPart.EyeR => 3, BodyPart.Thorax => 18, BodyPart.Abdomen => 14,
            BodyPart.ArmL or BodyPart.ArmR => 7, BodyPart.LegL or BodyPart.LegR => 8, _ => 4 // 손·발
        };
        public static BodyPart? Parent(BodyPart p) => p switch
        {
            BodyPart.HandL => BodyPart.ArmL, BodyPart.HandR => BodyPart.ArmR, BodyPart.FootL => BodyPart.LegL, BodyPart.FootR => BodyPart.LegR, _ => null
        };
        public static bool Vital(BodyPart p) => p == BodyPart.Head || p == BodyPart.Thorax || p == BodyPart.Abdomen;
        public static float PainFactor(WoundKind k) => k switch
        { WoundKind.Bruise => .8f, WoundKind.Burn => 1.4f, WoundKind.Acid => 1.3f, WoundKind.Stab => 1.2f, WoundKind.Scar => .2f, WoundKind.Missing => .3f, WoundKind.Surgical => .6f, _ => 1f };
        public static float BleedFactor(Wound w) => w.kind switch
        { WoundKind.Cut => 1f, WoundKind.Stab => 1.4f, WoundKind.Acid => .2f, WoundKind.Missing => w.age < FreshMissingSeconds ? 2f : 0f, WoundKind.Surgical => .3f, _ => 0f };
        // 장수용 약품 3등급(이름·재료 미정). 처치 품질 배율.
        public static readonly ResourceType[] Medicines = { ResourceType.MedicineBasic, ResourceType.MedicineNormal, ResourceType.MedicineAdvanced };
        public static float MedicineFactor(int tier) => tier switch { 0 => 1f, 1 => 1.3f, 2 => 1.6f, _ => .55f }; // -1 = 약 없음
        public static string PartName(BodyPart p) => p switch
        {
            BodyPart.Head => "머리(뇌)", BodyPart.EyeL => "왼눈", BodyPart.EyeR => "오른눈", BodyPart.Thorax => "가슴", BodyPart.Abdomen => "배",
            BodyPart.ArmL => "왼팔", BodyPart.ArmR => "오른팔", BodyPart.HandL => "왼손", BodyPart.HandR => "오른손",
            BodyPart.LegL => "왼다리", BodyPart.LegR => "오른다리", BodyPart.FootL => "왼발", _ => "오른발"
        };
        public static string WoundName(WoundKind k) => k switch
        { WoundKind.Cut => "베임", WoundKind.Stab => "찔림", WoundKind.Bruise => "타박상", WoundKind.Burn => "화상", WoundKind.Acid => "산성 손상", WoundKind.Scar => "흉터", WoundKind.Missing => "상실", _ => "수술 상처" };
        public static string IllnessName(IllnessKind k) => k == IllnessKind.Mold ? "곰팡이 감염" : "상처 감염";
        public static string MedicineName(MedicineLevel m) => m switch
        { MedicineLevel.NoCare => "치료 안 함", MedicineLevel.NoMedicine => "약 없이", MedicineLevel.Basic => "기초까지", MedicineLevel.Normal => "일반까지", _ => "고급까지" };

        // 쓰러질 때 등 무작위 외상. minor = 가벼운 상처만(결투·자해).
        public static void AddRandomWound(HealthState h, CommanderTraits traits, bool minor = false)
        {
            var parts = (BodyPart[])Enum.GetValues(typeof(BodyPart));
            var part = parts[UnityEngine.Random.Range(0, parts.Length)];
            if (h.Missing(part)) part = BodyPart.Thorax;
            var kinds = new[] { WoundKind.Cut, WoundKind.Stab, WoundKind.Bruise, WoundKind.Burn, WoundKind.Acid };
            var severe = traits != null && traits.Has(CommanderTrait.Robust) ? .6f : traits != null && traits.Has(CommanderTrait.Frail) ? 1.4f : 1f;
            var share = minor ? UnityEngine.Random.Range(.1f, .25f) : UnityEngine.Random.Range(.2f, .6f) * severe;
            // 쓰러짐 부상으로 바로 치명상을 만들지 않는다(사망은 출혈·사망 모드 판정이 정한다).
            AddWound(h, part, kinds[UnityEngine.Random.Range(0, kinds.Length)], Mathf.Min(MaxHp(part) * share, MaxHp(part) * (Vital(part) ? .6f : .9f)));
        }
        public static Wound AddWound(HealthState h, BodyPart part, WoundKind kind, float damage)
        {
            var w = new Wound { part = part, kind = kind, damage = Mathf.Max(0, damage) };
            h.wounds.Add(w); return w;
        }

        // 시간 경과: 출혈·회복·흉터·감염·질병. dt 초. gentle = 관대 사망 모드(죽지 않게 상한).
        public static void Tick(HealthState h, float dt, bool resting, bool gentle)
        {
            h.bloodLoss = Mathf.Clamp01(h.bloodLoss + h.BleedRate * dt);
            if (!h.Bleeding) h.bloodLoss = Mathf.Max(0, h.bloodLoss - BloodRegen * dt * (resting ? 1.5f : 1f));
            foreach (var w in h.wounds.ToArray())
            {
                w.age += dt;
                if (w.Permanent) continue;
                var heal = (w.tended ? HealTended * (.5f + w.quality) : HealUntended) * (resting ? 1.5f : 1f) * dt;
                var before = w.damage; w.damage = Mathf.Max(0, w.damage - heal);
                if (!w.infectionRolled && w.kind != WoundKind.Bruise && (w.tended || w.age >= InfectionAfter))
                {
                    w.infectionRolled = true;
                    if (Roll() < InfectionChance * (1 - (w.tended ? w.quality : 0)) && !h.illnesses.Any(i => i.kind == IllnessKind.WoundInfection && i.part == w.part))
                        h.illnesses.Add(new Illness { kind = IllnessKind.WoundInfection, part = w.part });
                }
                if (w.damage <= 0)
                {
                    h.wounds.Remove(w);
                    if (w.kind != WoundKind.Bruise && w.kind != WoundKind.Surgical && before >= 0 && w.age > 0 && Roll() < ScarChance * (w.tended ? 1 - w.quality * .5f : 1))
                        h.wounds.Add(new Wound { part = w.part, kind = WoundKind.Scar, damage = MaxHp(w.part) * .1f, tended = true });
                }
            }
            foreach (var i in h.illnesses.ToArray())
            {
                // 처치(투약)가 유효한 동안 악화를 늦춘다. 면역이 다 차면 회복.
                var rate = i.kind == IllnessKind.Mold ? .003f : .004f;
                i.severity = Mathf.Clamp01(i.severity + rate * (1 - (i.tendLeft > 0 ? i.quality * .8f : 0)) * dt - (i.immunity >= 1 ? .02f * dt : 0));
                i.immunity = Mathf.Min(1, i.immunity + (i.kind == IllnessKind.Mold ? .0045f : .005f) * (resting ? 1.3f : 1f) * dt);
                i.tendLeft = Mathf.Max(0, i.tendLeft - dt);
                if (i.immunity >= 1 && i.severity <= 0) h.illnesses.Remove(i);
            }
            if (gentle)
            {
                h.bloodLoss = Mathf.Min(h.bloodLoss, .9f);
                foreach (var i in h.illnesses) i.severity = Mathf.Min(i.severity, .95f);
                foreach (BodyPart p in new[] { BodyPart.Head, BodyPart.Thorax, BodyPart.Abdomen })
                    while (h.PartFraction(p) <= .05f && h.wounds.Any(w => w.part == p && !w.Permanent)) h.wounds.First(w => w.part == p && !w.Permanent).damage *= .8f;
            }
        }

        // 처치 품질 = 의료 능력 · 약품 · 시설(병상) · 치료자 상태. 0~1.
        public static float TendQuality(int medicineSkill, int medicineTier, bool inBed, float doctorManipulation, bool self)
        {
            var q = (.25f + medicineSkill * .035f) * MedicineFactor(medicineTier) + (inBed ? .1f : 0);
            if (medicineTier < 0) q = Mathf.Min(q, .5f); // 약 없는 처치는 품질 제한
            return Mathf.Clamp01(q * Mathf.Clamp01(doctorManipulation) * (self ? SelfTendFactor : 1));
        }
        // 허용 범위 안에서 가장 좋은 재고 약품 등급. -1 = 약 없음.
        public static int BestMedicine(MedicineLevel level, Func<ResourceType, int> stock)
        {
            for (var t = Mathf.Min(2, (int)level - 2); t >= 0; t--) if (stock(Medicines[t]) > 0) return t;
            return -1;
        }
        // 처치 1회: 미처치 상처 전부 + 처치 시기가 된 질병. 약이 필요한 투약(질병)은 약이 있을 때만.
        public static bool ApplyTend(HealthState h, float quality, bool medicine)
        {
            var any = false;
            foreach (var w in h.wounds.Where(w => w.NeedsTend)) { w.tended = true; w.quality = quality; any = true; }
            if (medicine) foreach (var i in h.illnesses.Where(i => i.NeedsTend)) { i.quality = quality; i.tendLeft = IllnessTendInterval; any = true; }
            return any;
        }
        public static bool NeedsDoctor(HealthState h, bool medicineAvailable)
            => h.medicine != MedicineLevel.NoCare && (h.wounds.Any(w => w.NeedsTend) || medicineAvailable && h.illnesses.Any(i => i.NeedsTend));

        // 옛 저장(6부위 경상/중상/영구 + 곰팡이)을 새 체계로 한 번 옮긴다.
        public static void Migrate(CommanderPersonalState p)
        {
            var h = p.health ??= new HealthState();
            if (h.migrated) return;
            h.migrated = true;
            foreach (var old in p.injuries)
            {
                BodyPart part;
                switch (old.part)
                {
                    case InjuryPart.LegacyAntenna: part = BodyPart.Head; break;
                    case InjuryPart.Arms: part = BodyPart.ArmL; break;
                    case InjuryPart.Legs: part = BodyPart.LegL; break;
                    case InjuryPart.Thorax: part = BodyPart.Thorax; break;
                    case InjuryPart.Head: part = BodyPart.Head; break;
                    default: continue; // 날개는 장비(신체 부상 아님)
                }
                if (old.severity == InjurySeverity.Permanent) h.wounds.Add(new Wound { part = part, kind = WoundKind.Scar, damage = MaxHp(part) * .1f, tended = true });
                else h.wounds.Add(new Wound { part = part, kind = WoundKind.Bruise, damage = MaxHp(part) * (old.severity == InjurySeverity.Minor ? .2f : .45f), tended = p.treating, quality = .4f, infectionRolled = true });
            }
            p.injuries.Clear();
            if (p.infected) h.illnesses.Add(new Illness { kind = IllnessKind.Mold, part = BodyPart.Thorax, severity = .2f });
            p.infected = false;
        }
    }

    // 수술 요청(F). 준비 중에는 물자를 예약만 하고 시작할 때 소비한다. 시작 후 취소 불가.
    public enum SurgeryKind { Amputate, Regenerate }
    public enum SurgeryStage { Waiting, Started, Done, Failed }
    [Serializable] public class SurgeryOrder
    {
        public SurgeryKind kind; public BodyPart part; public SurgeryStage stage;
        public string surgeonId = ""; // 직접 지정(빈 값 = 자동)
        public float work, weighted; // 진행 작업량, Σ(작업량 × 집도의 의료 능력)
        public string result = "";
        public bool Active => stage == SurgeryStage.Waiting || stage == SurgeryStage.Started;
    }
}

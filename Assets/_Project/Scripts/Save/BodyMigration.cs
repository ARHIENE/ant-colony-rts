using System.Collections.Generic;
using System.Linq;
using AntColony.Units;

namespace AntColony.Save
{
    // v17 → v18(2026-10-18 캐릭터 전환): 개미 15부위를 두 팔·두 다리 13부위로 옮긴다.
    // 사용자 승인 규칙: 큰턱 → 손, 다리 1·3·5 → 왼다리, 2·4·6 → 오른다리, 더듬이 → 머리(흉터·상실은 머리 흉터).
    // 손상은 부위 최대치 비율을 유지하고, 합쳐진 부위는 쓰러짐 부상과 같은 상한(생명 부위 60%, 나머지 90%)으로 자른다.
    // 상실은 보존한다. 조용히 완치하지 않는다.
    public static class BodyMigration
    {
        // 옛 BodyPart 숫자 → (새 부위, 옛 최대 체력)
        private static readonly (BodyPart part, float oldMax)[] Map =
        {
            (BodyPart.Head, 10), (BodyPart.EyeL, 3), (BodyPart.EyeR, 3), (BodyPart.Head, 3), (BodyPart.Head, 3),
            (BodyPart.HandL, 5), (BodyPart.HandR, 5), (BodyPart.Thorax, 18),
            (BodyPart.LegL, 6), (BodyPart.LegR, 6), (BodyPart.LegL, 6), (BodyPart.LegR, 6), (BodyPart.LegL, 6), (BodyPart.LegR, 6),
            (BodyPart.Abdomen, 14)
        };
        private static bool Antenna(int old) => old == 3 || old == 4;

        public static void Upgrade(SaveFileV1 file)
        {
            foreach (var c in file.commanders ?? new List<CommanderDto>()) Health(c?.personalState);
            foreach (var b in file.buildings ?? new List<BuildingDto>())
                if (b?.prisoners != null) foreach (var p in b.prisoners) Health(p?.personalState);
            if (file.diplomacy?.civilizations != null && file.diplomacy.markets != null)
                foreach (var f in file.diplomacy.civilizations.Concat(file.diplomacy.markets))
                {
                    if (f?.prisoners != null) foreach (var p in f.prisoners) Health(p?.PersonalState);
                    if (f?.rebels != null) foreach (var p in f.rebels) Health(p?.PersonalState);
                }
            foreach (var h in file.diplomacy?.homecomings ?? new List<World.Homecoming>()) Health(h?.commander?.PersonalState);
            foreach (var r in file.diplomacy?.rebelMembers ?? new List<World.RebelMember>()) Health(r?.commander?.PersonalState);
            file.version = 18;
        }

        public static void Health(CommanderPersonalState s)
        {
            var h = s?.health;
            if (h == null) return;
            foreach (var w in h.wounds)
            {
                var old = (int)w.part;
                if (old < 0 || old >= Map.Length) { w.part = BodyPart.Thorax; continue; }
                var (part, oldMax) = Map[old];
                if (Antenna(old) && w.Permanent) { w.kind = WoundKind.Scar; w.damage = MedicalRules.MaxHp(part) * .1f; }
                else w.damage *= MedicalRules.MaxHp(part) / oldMax;
                w.part = part;
            }
            // 한 부위로 합쳐진 상실은 하나만, 손상 합계는 상한 안으로.
            foreach (var g in h.wounds.GroupBy(w => w.part).ToList())
            {
                foreach (var extra in g.Where(w => w.kind == WoundKind.Missing).Skip(1).ToList()) h.wounds.Remove(extra);
                var live = g.Where(w => w.kind != WoundKind.Missing && h.wounds.Contains(w)).ToList();
                var cap = MedicalRules.MaxHp(g.Key) * (MedicalRules.Vital(g.Key) ? .6f : .9f);
                var sum = live.Sum(w => w.damage);
                if (sum > cap) foreach (var w in live) w.damage *= cap / sum;
            }
            foreach (var i in h.illnesses)
                i.part = (int)i.part >= 0 && (int)i.part < Map.Length ? Map[(int)i.part].part : BodyPart.Thorax;
            foreach (var o in h.surgeries)
                o.part = (int)o.part >= 0 && (int)o.part < Map.Length ? Map[(int)o.part].part : BodyPart.Thorax;
            // 더듬이 절단처럼 생명 부위로 옮겨진 대기 절단은 할 수 없으므로 취소(시작 전엔 물자 예약만 있음).
            h.surgeries.RemoveAll(o => o.kind == SurgeryKind.Amputate && MedicalRules.Vital(o.part) && o.stage == SurgeryStage.Waiting);
            foreach (var o in h.surgeries.Where(o => o.kind == SurgeryKind.Amputate && MedicalRules.Vital(o.part) && o.stage == SurgeryStage.Started))
            { o.stage = SurgeryStage.Failed; o.result = "캐릭터 전환으로 절단 부위가 없어져 중단"; }
        }
    }
}

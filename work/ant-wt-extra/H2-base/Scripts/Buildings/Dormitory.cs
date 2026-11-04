using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;

namespace AntColony.Buildings
{
    // 숙소: 1동 정원 4명. 밤에 장수가 자러 온다. 배정은 자동(부부는 같은 숙소, 라이벌은 피함).
    // 배정은 저장하지 않는다. 불러오면 다음 잠자리에서 다시 정한다.
    public sealed class Dormitory : BuildingBase
    {
        private static readonly List<Dormitory> Active = new List<Dormitory>();
        private readonly List<CommanderAnt> residents = new List<CommanderAnt>();
        public IReadOnlyList<CommanderAnt> Residents => residents;
        public static IReadOnlyList<Dormitory> All => Active;
        // 침대 수: 숙소 건물 4 / 자리·침대·해먹 1 / 큰침대·2층침대 2(2026-10-06). 방 판정에서는 숙소 건물 4, 2층침대 2, 나머지 1로 센다(큰침대 혼자 = 개인실).
        public int Beds => Data == null ? GameBalance.DormitoryBeds : Data.kind switch
        {
            BuildingKind.SleepingMat or BuildingKind.SingleBed or BuildingKind.Hammock => 1,
            BuildingKind.DoubleBed or BuildingKind.BunkBed => 2,
            _ => GameBalance.DormitoryBeds
        };
        public int RoomBedCount => Data == null || Data.kind == BuildingKind.Dormitory ? GameBalance.DormitoryBeds : Data.kind == BuildingKind.BunkBed ? 2 : 1;
        public bool IsMat => Data != null && Data.kind == BuildingKind.SleepingMat;
        public bool IsHammock => Data != null && Data.kind == BuildingKind.Hammock;
        public bool HasRoom => residents.Count < Beds;
        protected override void OnEnable() { base.OnEnable(); Active.Add(this); }
        protected override void OnDisable() { Active.Remove(this); residents.Clear(); base.OnDisable(); }

        public static Dormitory Of(CommanderAnt c) => Active.FirstOrDefault(d => d != null && !d.IsDead && d.residents.Contains(c));

        // 이미 자리가 있으면 그대로, 없으면 배우자 숙소 → 라이벌 없는 숙소 → 아무 빈 숙소 순. 없으면 null(노숙).
        public static Dormitory Assign(CommanderAnt c)
        {
            foreach (var d in Active) d.residents.RemoveAll(r => r == null || r.IsDead || !r.IsColonyMember);
            var current = Of(c);
            if (current != null) return current;
            var open = Active.Where(d => d != null && d.isActiveAndEnabled && !d.IsDead && d.HasRoom).OrderBy(d => (d.Position - c.Position).sqrMagnitude).ToList();
            bool Has(Dormitory d, System.Func<CommanderRelation, bool> test) =>
                d.residents.Any(r => c.PersonalState.relations.Exists(x => x.otherId == r.PersonalState.id && test(x)));
            // 큰침대는 부부를 먼저: 배우자가 없는 장수는 큰침대를 마지막에 고른다.
            var married = c.PersonalState.relations.Exists(x => x.spouse);
            if (!married) open = open.OrderBy(d => d.Data != null && d.Data.kind == BuildingKind.DoubleBed).ToList();
            var pick = open.FirstOrDefault(d => Has(d, r => r.spouse))
                ?? (married ? open.FirstOrDefault(d => d.Data != null && d.Data.kind == BuildingKind.DoubleBed && d.residents.Count == 0) : null)
                ?? open.FirstOrDefault(d => !Has(d, r => r.value <= SocialRules.Rival))
                ?? open.FirstOrDefault();
            pick?.residents.Add(c);
            return pick;
        }

        public bool LivesWithRival(CommanderAnt c) =>
            residents.Any(r => r != c && c.PersonalState.relations.Exists(x => x.otherId == r.PersonalState.id && x.value <= SocialRules.Rival));
    }
}

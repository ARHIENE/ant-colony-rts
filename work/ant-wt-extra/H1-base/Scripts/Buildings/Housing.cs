using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;

namespace AntColony.Buildings
{
    // Phase 4 주거 건물: 일반개미 살 자리(인구 상한). 가구가 아니라 건물 단위, 종류별 수용 수 고정(잠정).
    public sealed class Housing : BuildingBase
    {
        private static readonly List<Housing> Active = new List<Housing>();
        public int Capacity => Data == null ? 0 : CapacityOf(Data.kind);
        public static int CapacityOf(BuildingKind k) => k switch { BuildingKind.Hut => GameBalance.HutHousing, BuildingKind.House => GameBalance.HouseHousing, BuildingKind.Apartment => GameBalance.ApartmentHousing, _ => 0 };
        public static float Productivity(BuildingKind k) => k switch { BuildingKind.House => GameBalance.HouseProductivity, BuildingKind.Apartment => GameBalance.ApartmentProductivity, _ => GameBalance.HutProductivity };
        public static bool IsKind(BuildingKind k) => k == BuildingKind.Hut || k == BuildingKind.House || k == BuildingKind.Apartment;
        public static IEnumerable<Housing> All => Active.Where(h => h != null && !h.IsDead);
        public static int TotalCapacity => Active.Where(h => h != null && !h.IsDead).Sum(h => h.Capacity);
        protected override void OnEnable() { base.OnEnable(); Active.Add(this); }
        protected override void OnDisable() { Active.Remove(this); base.OnDisable(); }
    }
}

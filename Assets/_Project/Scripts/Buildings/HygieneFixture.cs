using System.Collections.Generic;
using AntColony.Data;
using AntColony.Units;

namespace AntColony.Buildings
{
    // 위생 가구(2026-10-05): 화장실·세면대·샤워기. 한 번에 1명이 쓰고 위생 욕구를 채운다(CommanderHygiene).
    // 이용자는 저장하지 않는다. 불러오면 쓰던 장수는 제자리에서 마저 쓴다.
    public sealed class HygieneFixture : BuildingBase
    {
        private static readonly List<HygieneFixture> Active = new List<HygieneFixture>();
        public static IReadOnlyList<HygieneFixture> All => Active;
        public CommanderAnt User { get; private set; }
        // 0 화장실 / 1 세면대 / 2 샤워기 (GameBalance.WashHygiene 순서)
        public int KindIndex => Data.kind == BuildingKind.Washbasin ? 1 : Data.kind == BuildingKind.Shower ? 2 : 0;
        public bool Free => isActiveAndEnabled && !IsDead && (User == null || User.IsDead || User.WashSpot != this);

        protected override void OnEnable() { base.OnEnable(); Active.Add(this); }
        protected override void OnDisable() { Active.Remove(this); User = null; base.OnDisable(); }

        public bool Join(CommanderAnt c)
        {
            if (User == c) return true;
            if (!Free) return false;
            User = c; return true;
        }
        public void Leave(CommanderAnt c) { if (User == c) User = null; }
    }
}

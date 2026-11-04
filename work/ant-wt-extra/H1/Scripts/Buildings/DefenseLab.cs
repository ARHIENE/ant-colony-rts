using System.Collections.Generic;

namespace AntColony.Buildings
{
    // 방어시설 연구소. 업그레이드 단계는 CampaignResearch 상태에 저장되고, 연구소가 하나라도 있어야 올릴 수 있다.
    public sealed class DefenseLab : BuildingBase
    {
        private static readonly List<DefenseLab> Active = new List<DefenseLab>();
        public static bool AnyActive => Active.Exists(l => l != null && !l.IsDead);
        protected override void OnEnable() { base.OnEnable(); Active.Add(this); }
        protected override void OnDisable() { Active.Remove(this); base.OnDisable(); }
    }
}

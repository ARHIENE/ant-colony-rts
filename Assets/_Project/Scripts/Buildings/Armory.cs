using System.Collections.Generic;

namespace AntColony.Buildings
{
    // 무기고(2026-10-05): 장비 보관함 한도를 늘린다(1동 +10, 잠정). 놓인 방은 무기고. 무기고가 부서져 한도를 넘어도 장비는 사라지지 않고 새로 넣지만 못 한다.
    public sealed class Armory : BuildingBase
    {
        private static readonly List<Armory> Active = new List<Armory>();
        public static int Count => Active.Count;
        protected override void OnEnable() { base.OnEnable(); Active.Add(this); }
        protected override void OnDisable() { Active.Remove(this); base.OnDisable(); }
    }
}

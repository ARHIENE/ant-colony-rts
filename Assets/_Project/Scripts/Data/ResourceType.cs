namespace AntColony.Data
{
    public static class ResourceLabels
    {
        public static string DisplayName(this ResourceType type) => type switch
        {
            ResourceType.Food => "식량", ResourceType.Soil => "흙", ResourceType.Special => "특수 자원",
            ResourceType.Stone => "돌", ResourceType.Sand => "모래", ResourceType.Clay => "점토", ResourceType.Wood => "목재", ResourceType.Leaf => "잎",
            ResourceType.Fiber => "식물 섬유", ResourceType.Resin => "수지", ResourceType.Chitin => "갑각", ResourceType.Cobweb => "거미줄", ResourceType.Wax => "밀랍",
            ResourceType.IronOre => "철광석", ResourceType.CopperOre => "구리광석", ResourceType.Coal => "석탄", ResourceType.Sulfur => "유황",
            ResourceType.Plank => "판재", ResourceType.StoneBlock => "석재 블록", ResourceType.Brick => "벽돌", ResourceType.Glass => "유리", ResourceType.Cloth => "직물",
            ResourceType.Iron => "철", ResourceType.Copper => "구리", ResourceType.Steel => "강철",
            ResourceType.Honeydew => "감로", ResourceType.Thread => "실", ResourceType.Honey => "꿀",
            ResourceType.AnimalMedicine => "동물용 의약품", ResourceType.SurgeryKit => "동물 수술 키트",
            _ => type.ToString()
        };
        // 식량·특수 외에는 모두 '재료'다. 재료 종류마다 저장 한도는 기존 재료(흙) 한도를 같이 쓴다.
        public static bool IsMaterial(this ResourceType type) => type != ResourceType.Food && type != ResourceType.Special;
        // 누적 기록·외교는 기존 3칸(식량·재료·특수)으로 묶는다.
        public static int Group(this ResourceType type) => type == ResourceType.Food ? 0 : type == ResourceType.Special ? 2 : 1;
        public static readonly ResourceType[] Materials = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(
            (ResourceType[])System.Enum.GetValues(typeof(ResourceType)), t => t.IsMaterial()));
    }

    // 2026-10-10 자원 분화: 원재료·가공품·목장 생산물. 저장 번호가 밀리지 않게 기존 3종 뒤에 붙인다.
    // ponytail: 목록은 기획 후보(최종 미정)에서 고른 잠정안. 확정되면 여기와 MaterialInfo 표만 고친다.
    public enum ResourceType
    {
        Food,
        Soil,
        Special,
        // 원재료
        Stone, Sand, Clay, Wood, Leaf, Fiber, Resin, Chitin, Cobweb, Wax, IronOre, CopperOre, Coal, Sulfur,
        // 가공품(기본 2단계: 원재료→가공품, 고급 3단계: 광석→금속→강철)
        Plank, StoneBlock, Brick, Glass, Cloth, Iron, Copper, Steel,
        // 목장 생산물
        Honeydew, Thread, Honey,
        // 2026-10-11 동물 치료(장수용 약과 별도 자원)
        AnimalMedicine, SurgeryKit
    }

    // 재료 특성(내구·무게·가연성·미관·단열). 후반 재료가 모든 면에서 우월하지 않게 용도를 나눈다.
    public readonly struct MaterialInfo
    {
        public readonly float durability, weight, beauty, insulation; public readonly bool flammable, rare, structural;
        public MaterialInfo(float durability, float weight, float beauty, float insulation, bool flammable, bool rare, bool structural)
        { this.durability = durability; this.weight = weight; this.beauty = beauty; this.insulation = insulation; this.flammable = flammable; this.rare = rare; this.structural = structural; }

        // ponytail: 재료별 수치는 기획 미정(잠정). durability = 건물 체력 배율, beauty = 미관 점수(-2~+4), insulation = 단열(0~1).
        public static MaterialInfo For(ResourceType t) => t switch
        {
            ResourceType.Soil => new MaterialInfo(1f, 1f, -1, .5f, false, false, true),
            ResourceType.Stone => new MaterialInfo(1.6f, 2f, 0, .6f, false, false, true),
            ResourceType.Clay => new MaterialInfo(1.1f, 1.2f, 0, .6f, false, false, true),
            ResourceType.Wood => new MaterialInfo(.9f, .8f, 1, .7f, true, false, true),
            ResourceType.Leaf => new MaterialInfo(.5f, .3f, 0, .3f, true, false, true),
            ResourceType.Chitin => new MaterialInfo(1.3f, .7f, 1, .4f, false, false, true),
            ResourceType.Plank => new MaterialInfo(1.1f, .8f, 2, .7f, true, false, true),
            ResourceType.StoneBlock => new MaterialInfo(2f, 2f, 2, .7f, false, false, true),
            ResourceType.Brick => new MaterialInfo(1.6f, 1.6f, 2, .8f, false, false, true),
            ResourceType.Glass => new MaterialInfo(.6f, 1f, 4, .2f, false, true, true),
            ResourceType.Cloth => new MaterialInfo(.4f, .2f, 3, .9f, true, false, true),
            ResourceType.Iron => new MaterialInfo(2.2f, 2.5f, 1, .2f, false, true, true),
            ResourceType.Copper => new MaterialInfo(1.8f, 2.4f, 3, .1f, false, true, true),
            ResourceType.Steel => new MaterialInfo(3f, 2.4f, 2, .2f, false, true, true),
            ResourceType.Wax => new MaterialInfo(.6f, .5f, 3, .6f, true, true, true),
            _ => new MaterialInfo(1f, 1f, 0, .5f, false, false, false)
        };
        public static ResourceType[] Structural => System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(ResourceLabels.Materials, m => For(m).structural));
    }
}

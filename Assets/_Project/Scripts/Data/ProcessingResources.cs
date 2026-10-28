namespace AntColony.Data
{
    public static class ProcessingResources
    {
        public static string Model(ResourceType t) => t switch
        {
            ResourceType.Wheat => "Grain_Wheat", ResourceType.Barley => "Grain_Barley", ResourceType.Rice => "Grain_Rice", ResourceType.Grapes => "Fruit_Grape", ResourceType.SunflowerSeed => "Nut_Sunflower",
            ResourceType.WheatFlour => "Proc_WheatFlour", ResourceType.SunflowerOil => "Proc_SunflowerOil", ResourceType.RubberSap => "Gather_RubberSap", ResourceType.Rubber => "Proc_Rubber",
            ResourceType.Beer => "Alc_Beer", ResourceType.RiceWine => "Alc_RiceWine", ResourceType.FruitWine => "Alc_FruitWine", ResourceType.Mead => "Alc_Mead", ResourceType.HoneydewWine => "Alc_HoneydewWine", ResourceType.Spirits => "Alc_Spirits", _ => null
        };
        public static bool Alcohol(ResourceType t) => t is ResourceType.Beer or ResourceType.RiceWine or ResourceType.FruitWine or ResourceType.Mead or ResourceType.HoneydewWine or ResourceType.Spirits;
        public static bool Food(ResourceType t) => t is ResourceType.Wheat or ResourceType.Barley or ResourceType.Rice or ResourceType.Grapes or ResourceType.SunflowerSeed or ResourceType.WheatFlour or ResourceType.SunflowerOil || Alcohol(t);
        public static string Name(ResourceType t) => t switch
        {
            ResourceType.Wheat => "밀", ResourceType.Barley => "보리", ResourceType.Rice => "쌀", ResourceType.Grapes => "포도", ResourceType.SunflowerSeed => "해바라기씨",
            ResourceType.WheatFlour => "밀가루", ResourceType.SunflowerOil => "해바라기유", ResourceType.RubberSap => "고무 수액", ResourceType.Rubber => "고무",
            ResourceType.Beer => "맥주", ResourceType.RiceWine => "쌀술", ResourceType.FruitWine => "과실주", ResourceType.Mead => "벌꿀주", ResourceType.HoneydewWine => "감로주", ResourceType.Spirits => "증류주", _ => null
        };
    }
}

namespace AntColony.Data
{
    public static class ResourceLabels
    {
        public static string DisplayName(this ResourceType type) => type switch
        {
            ResourceType.Food => "식량", ResourceType.Soil => "재료", ResourceType.Special => "특수 자원", _ => type.ToString()
        };
    }

    public enum ResourceType
    {
        Food,
        Soil,
        Special
    }
}

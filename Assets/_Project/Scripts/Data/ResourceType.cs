namespace AntColony.Data
{
    public static class ResourceLabels
    {
        public static string DisplayName(this ResourceType type) => type == ResourceType.Soil ? "재료" : type.ToString();
    }

    public enum ResourceType
    {
        Food,
        Soil,
        Special
    }
}

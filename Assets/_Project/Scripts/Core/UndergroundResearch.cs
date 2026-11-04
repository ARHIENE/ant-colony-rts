namespace AntColony.Core
{
    public static class UndergroundResearch
    {
        public static ScienceDefinition[] Definitions() => new[] {
            new ScienceDefinition(ScienceTechnology.UndergroundSurvey, "지하 탐사", 4) };
    }
}

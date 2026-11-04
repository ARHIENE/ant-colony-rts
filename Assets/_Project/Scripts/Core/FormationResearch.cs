using System.Collections.Generic;
using AntColony.Units;

namespace AntColony.Core
{
    public static class FormationResearch
    {
        public static ScienceTechnology Technology(Formation f) => (ScienceTechnology)((int)ScienceTechnology.FormationLine + (int)f - (int)Formation.Line);
        public static IEnumerable<ScienceDefinition> Definitions()
        {
            for (var f = Formation.Line; f <= Formation.Focus; f++)
                yield return new ScienceDefinition(Technology(f), FormationRules.Name(f) + " 전술", f <= Formation.Wedge ? 2 : 3);
        }
        public static bool Unlocked(Formation f) => f >= Formation.Line && f <= Formation.Focus
            && CampaignResearch.Instance?.Has(Technology(f)) == true;
    }
}

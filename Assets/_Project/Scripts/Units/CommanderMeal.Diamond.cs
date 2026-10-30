using System.Linq;
using AntColony.Core;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        internal static bool DiamondMealAvailable(CommanderPersonalState state) => !state.moodFactors.Any(f => f.reason == "식선: 잊지 못할 한 끼" && f.remaining > 0);
        private void ApplyDiamondMeal()
        {
            if (DiamondMealAvailable(personalState)) personalState.AddMood("식선: 잊지 못할 한 끼", 20, GameCalendar.SecondsPerDay);
        }
    }
}

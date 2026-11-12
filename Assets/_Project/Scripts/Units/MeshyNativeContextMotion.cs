namespace AntColony.Units
{
    // Visual choice only; health, navigation and combat timings remain simulation-owned.
    public static class MeshyNativeContextMotion
    {
        public static string Resolve(string action, string idle, float mobility, bool leisureWalk)
        {
            if (idle != null && (action == "Idle_A" || action != null && action.StartsWith("IdleVariant"))) return idle;
            if (action == "Walk" || action == "Run" || action == "WalkInjured")
            {
                if (mobility < .45f) return "WalkLimpSevere";
                if (mobility < .85f) return "WalkLimp";
                if (leisureWalk && action != "WalkInjured") return "LeisureWalk";
            }
            return action;
        }
    }
}

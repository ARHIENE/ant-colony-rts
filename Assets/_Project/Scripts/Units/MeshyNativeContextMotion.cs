namespace AntColony.Units
{
    // Choices remain stable for a character; no per-frame random clip changes.
    public static class MeshyNativeContextMotion
    {
        public static string Resolve(string action, string idle, float mobility, bool leisureWalk, int variant = 0)
        {
            uint seed = unchecked((uint)variant);
            if (idle != null && (action == "Idle_A" || action != null && action.StartsWith("IdleVariant"))) action = idle;
            if (action == "Walk" || action == "Run" || action == "WalkInjured")
            {
                if (mobility < .45f) return "WalkLimpSevere";
                if (mobility < .85f) return seed % 2 == 0 ? "WalkLimp" : "WalkLimpAlternate";
                if (leisureWalk && action != "WalkInjured") return "LeisureWalk";
                if (action == "Run") return "RunVariant" + seed % 3;
            }
            if (action == "IdleTalk" || action == "Talk") return "TalkVariant" + seed % 4;
            if (action == "Listen") return seed % 2 == 0 ? "Listen" : "ListenAgree";
            if (action == "Celebrate") return "CelebrateVariant" + seed % 4;
            if (action != null && action.StartsWith("IdleVariant")) return "StandVariant" + seed % 11;
            return action;
        }
    }
}

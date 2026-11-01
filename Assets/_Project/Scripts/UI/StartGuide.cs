namespace AntColony.UI
{
    // 호환 진입점도 같은 목표 사슬을 읽는다.
    public static class StartGuide
    {
        public static bool Active => Core.GameSession.Exists && Core.GameSession.Instance.GameStarted && GoalChain.CurrentStage < 3;
        public static string Text() => GoalChain.CurrentText;
    }
}

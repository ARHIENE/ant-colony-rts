using UnityEngine;

namespace AntColony.Core
{
    // 새 게임 화면에서 고르는 값. 저장 파일에 그대로 들어가고 불러올 때 같은 맵을 다시 만든다.
    public enum MapSize { Small, Medium, Large }

    // 침공 빈도/강도만 바꾼다. Normal은 기존 기본 동작과 완전히 같다(배수 1.0).
    public enum DifficultyLevel { Gentle, Normal, Harsh }
    public enum CommanderDeathMode { Gentle, Normal, Harsh }

    [System.Serializable]
    public class NewGameOptions
    {
        public MapSize mapSize = MapSize.Medium;
        public DifficultyLevel difficulty = DifficultyLevel.Normal;
        public CommanderDeathMode commanderDeath = CommanderDeathMode.Normal;
        public int seed = 12345;

        public NewGameOptions Clone() => new NewGameOptions { mapSize = mapSize, difficulty = difficulty, commanderDeath = commanderDeath, seed = seed };

        public static int RandomSeed() => UnityEngine.Random.Range(1, 999999);
    }

    public static class CommanderDeathRuntime
    {
        public static CommanderDeathMode Mode => GameSession.Exists ? GameSession.Instance.Options.commanderDeath : CommanderDeathMode.Normal;
    }

    // 맵 크기는 절대값이 아니라 씬에 들어 있는 원래 지형 한 변에 대한 배수다.
    // Medium = 원래 크기라서 기본 설정으로 시작하면 기존 플레이와 같은 넓이가 나온다.
    // 지형은 항상 본거지를 중심으로 다시 잡히므로, 작게 골라도 본거지가 맵 밖으로 나가지 않는다.
    public static class MapSizes
    {
        public static float Scale(MapSize size) => size switch
        {
            MapSize.Small => 0.6f,
            MapSize.Large => 1.6f,
            _ => 1f
        };

        public static string Label(MapSize size) => size switch
        {
            MapSize.Small => "Small (60% of base)",
            MapSize.Large => "Large (160% of base)",
            _ => "Medium (base size)"
        };
    }

    // 난이도가 실제로 건드리는 값만 모아 둔다. 새 랜덤 이벤트 체계는 추가하지 않았고,
    // 기존 침공(ColonyInvasion) / 방문자(LocalIncursions) / 편입 거점 습격(SettlementDefense)에만 적용한다.
    public static class DifficultyProfile
    {
        // 간격 배수: 클수록 뜸하게 온다.
        public static float IntervalScale(DifficultyLevel level) => level switch
        {
            DifficultyLevel.Gentle => 1.6f,
            DifficultyLevel.Harsh => 0.65f,
            _ => 1f
        };

        // 파동 병력 수 배수. 반올림 후 최소 1을 보장한다.
        public static float StrengthScale(DifficultyLevel level) => level switch
        {
            DifficultyLevel.Gentle => 0.6f,
            DifficultyLevel.Harsh => 1.5f,
            _ => 1f
        };

        public static string Label(DifficultyLevel level) => level switch
        {
            DifficultyLevel.Gentle => "Gentle",
            DifficultyLevel.Harsh => "Harsh",
            _ => "Normal"
        };

        // 문구는 실제 배수 그대로 적는다. 간격 x1.6 = 빈도 약 -37%, 간격 x0.65 = 빈도 약 +54%.
        public static string Description(DifficultyLevel level) => level switch
        {
            DifficultyLevel.Gentle => "Invasion gaps x1.6 (about 37% fewer waves), wave size x0.6.",
            DifficultyLevel.Harsh => "Invasion gaps x0.65 (about 54% more waves), wave size x1.5.",
            _ => "Baseline pacing. Identical to the pre-difficulty build."
        };
    }

    // 게임플레이 쪽에서 난이도 배수를 읽는 유일한 창구.
    // GameSession이 아직 없으면(기존 검사 스크립트 등) Normal과 똑같이 동작한다.
    public static class DifficultyRuntime
    {
        public static DifficultyLevel Level => GameSession.Exists ? GameSession.Instance.Options.difficulty : DifficultyLevel.Normal;
        public static float IntervalScale => DifficultyProfile.IntervalScale(Level);
        public static float StrengthScale => DifficultyProfile.StrengthScale(Level);

        public static int ScaleCount(int count) => Mathf.Max(1, Mathf.RoundToInt(count * StrengthScale));
    }
}

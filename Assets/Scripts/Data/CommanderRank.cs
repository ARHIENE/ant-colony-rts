namespace AntColony.Data
{
    // 장수의 관직. 관직이 곧 지휘 한도(배정 가능한 일반개미 수)를 정한다.
    public enum CommanderRank
    {
        Corporal,
        Sergeant,
        Lieutenant,
        Captain,
        General
    }

    public static class CommanderRanks
    {
        // ponytail: 관직별 지휘 한도는 잠정값이다. 확정 밸런스가 아니며 기획 확정 시 이 표만 고치면 된다.
        public static int CommandLimit(CommanderRank rank) => rank switch
        {
            CommanderRank.Corporal => 5,
            CommanderRank.Sergeant => 10,
            CommanderRank.Lieutenant => 20,
            CommanderRank.Captain => 35,
            _ => 50
        };
    }
}

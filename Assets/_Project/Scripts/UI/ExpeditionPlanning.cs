using AntColony.Core;
using AntColony.World;

namespace AntColony.UI
{
    public static class ExpeditionPlanning
    {
        public static string TransportName(ExpeditionTransport ship) => ship.name switch
        { "Vehicle" => "차량", "Aircraft" => "비행기", _ => ship.name };
        public static int LoadAmount(ExpeditionTransport ship, ExpeditionSite site) => ship == null ? 0
            : LoadAmount(ship.RecommendedFood(site), ship.AvailableFood, ship.ReturnCargoSpace,
                ResourceManager.Instance?.GetAmount(AntColony.Data.ResourceType.Food) ?? 0);
        public static int LoadAmount(int recommended, int available, int space, int stock) =>
            System.Math.Max(0, System.Math.Min(System.Math.Max(0, recommended - available), System.Math.Min(space, stock)));
        public static string DepartureText(ExpeditionTransport ship, ExpeditionSite site)
        {
            var block = ship.DepartureBlock(site);
            var time = ship.PlannedRoundTripSeconds(site);
            return (float.IsInfinity(time) ? "이동 불가" : $"왕복 {time / GameCalendar.SecondsPerDay:0.0}일(감속·접안 포함)")
                + (ship.CanOperate ? $" · 식량 {ship.AvailableFood}/{ship.RecommendedFood(site)}" : $" · 식량 {ship.AvailableFood} · 권장량 산정 불가")
                + $"\n왕복 + 현지·예비 각 1일 · 화물 여유 {ship.ReturnCargoSpace}\n"
                + (block != "" ? block : ship.DepartureWarning(site) is var warning && warning != "" ? warning : "출정 가능");
        }
    }
}

using AntColony.Core;
using AntColony.Units;

namespace AntColony.Buildings
{
    public sealed partial class FarmPlot
    {
        internal void NoteFarmer(CommanderAnt farmer)
        {
            Data.diamondCycle ??= new SoleWorker { mixed = !Node.NeedsSowing };
            var before = Data.diamondCycle.Empowered;
            Data.diamondCycle.Note(farmer.PersonalState.id, farmer.Traits.Has(CommanderTrait.DiamondFarming));
            if (before && !Data.diamondCycle.Empowered)
                Data.diamondReason = "낙토 해제 · 다른 장수 " + farmer.CommanderName + " 관여";
            else if (string.IsNullOrEmpty(Data.diamondReason) && !Data.diamondCycle.Empowered)
                Data.diamondReason = "낙토 미적용 · 전 과정 동일 보유자 필요";
        }
        public bool Blessed => Data.diamondCycle?.Empowered == true;
        public string DiamondStatus => Blessed ? "낙토 · 토양/지역/계절/온도 불이익 면제" : Data.diamondReason ?? "낙토 미적용 · 파종 담당 없음";
        private void NextDiamondCycle()
        {
            // 다년생 밭은 재파종하지 않으므로 첫 파종자의 자격을 다음 관리/수확 주기에도 보존한다.
            if (!Blessed) return;
            Data.diamondReason = null;
        }
    }
}

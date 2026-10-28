using AntColony.Core;
using AntColony.Data;
using UnityEngine;
namespace AntColony.Buildings
{
    public sealed partial class Processor
    {
        public bool Brewing => Data != null && Data.kind == BuildingKind.BrewingBarrel;
        public string FermentationStatus => state.fermenting ? $"발효 중 {state.fermentation / FoodProcessingRecipes.FermentSeconds:P0} · 상주 작업자 불필요"
            : state.fermented ? "발효 완료 · 장수 회수 대기" : "원료 배송·투입 작업 대기";
        private void Update()
        {
            if (!AntColony.Save.SaveSystem.Busy && GameSession.Exists && GameSession.Instance.GameStarted) TickFermentation(Time.deltaTime);
        }
        public void TickFermentation(float seconds)
        {
            if (!Brewing || !state.fermenting || Paused || IsDead || state.ruined || !isActiveAndEnabled || !(seconds > 0) || float.IsInfinity(seconds)) return;
            state.fermentation = Mathf.Min(FoodProcessingRecipes.FermentSeconds, state.fermentation + seconds);
            if (state.fermentation < FoodProcessingRecipes.FermentSeconds) return;
            state.fermenting = false; state.fermented = true;
        }
    }
}

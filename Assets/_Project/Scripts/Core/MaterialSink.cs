using AntColony.Data;
using UnityEngine;

namespace AntColony.Core
{
    // 창고에서 재료를 집어 가져다주는 배송 목적지(예정지·조리대·가공대·공방). WorkerAnt.CommandFetchFor가 같은 흐름으로 처리한다.
    public interface IMaterialSink
    {
        Vector3 Position { get; }
        bool Paused { get; }
        // 지금 가져와야 할 재료와 양. 없으면 false.
        bool NextPickup(out ResourceType type, out int amount);
        bool BeginPickup(object courier, ResourceType type, int amount);
        int Deliver(object courier, ResourceType type, int amount);
        void AbortPickup(object courier);
    }
    public interface IFreshMaterialSink : IMaterialSink
    {
        int DeliverFood(object courier, ResourceType type, int amount, FoodLots lots);
        void AgeIngredients(float seconds);
    }
}

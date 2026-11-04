using UnityEngine;
namespace AntColony.World
{
    // 층 전체가 이 노드 하나를 공유한다. 일반 장수 채집 경로는 ResourceNode에서 제외한다.
    public sealed class UndergroundLayer : MonoBehaviour
    {
        public int Id { get; internal set; }
    }
}

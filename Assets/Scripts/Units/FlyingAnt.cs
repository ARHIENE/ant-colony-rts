using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Units
{
    // 표준 RTS형 비행 유닛. NavMesh를 쓰지 않고 지면 위 일정 고도를 직선으로 이동하며 장애물을 무시한다.
    public class FlyingAnt : SoldierAnt, IAirborne
    {
        [SerializeField] private float flightAltitude = 3f;
        [SerializeField] private float arriveThreshold = 0.15f;

        private Vector3 flightDestination;

        public override void Initialize(UnitData data, ObjectPool sourcePool, GameObject prefab)
        {
            base.Initialize(data, sourcePool, prefab);

            // 비행 이동은 NavMesh를 쓰지 않으므로 에이전트를 끄고 직접 위치를 갱신한다.
            Agent.enabled = false;
            flightDestination = ToFlightPoint(transform.position);
        }

        protected override void Update()
        {
            base.Update();
            transform.position = Vector3.MoveTowards(
                transform.position,
                flightDestination,
                Data.moveSpeed * Time.deltaTime);
        }

        protected override void SetMoveDestination(Vector3 destination)
        {
            flightDestination = ToFlightPoint(destination);
        }

        protected override bool HasReachedDestination()
        {
            return (transform.position - flightDestination).sqrMagnitude <= arriveThreshold * arriveThreshold;
        }

        // 공격 사거리는 고도 차이를 빼고 XZ 평면 거리로만 판정한다.
        protected override float GetDistanceTo(Vector3 position)
        {
            var delta = position - transform.position;
            delta.y = 0f;
            return delta.magnitude;
        }

        private Vector3 ToFlightPoint(Vector3 groundPoint)
        {
            return new Vector3(groundPoint.x, groundPoint.y + flightAltitude, groundPoint.z);
        }
    }
}

using System.Collections.Generic;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Buildings
{
    public class BuildingBase : MonoBehaviour
    {
        private static readonly List<BuildingBase> DepositPoints = new List<BuildingBase>();

        [SerializeField] protected BuildingData data;

        public BuildingData Data => data;

        protected virtual bool IsDepositPoint => false;

        protected virtual void OnEnable()
        {
            if (IsDepositPoint)
            {
                DepositPoints.Add(this);
            }
        }

        protected virtual void OnDisable()
        {
            DepositPoints.Remove(this);
        }

        public static BuildingBase FindNearestDepositPoint(Vector3 from)
        {
            BuildingBase nearest = null;
            var nearestDistSqr = float.MaxValue;
            foreach (var point in DepositPoints)
            {
                if (point == null) continue;
                var distSqr = (point.transform.position - from).sqrMagnitude;
                if (distSqr < nearestDistSqr)
                {
                    nearestDistSqr = distSqr;
                    nearest = point;
                }
            }
            return nearest;
        }
    }
}

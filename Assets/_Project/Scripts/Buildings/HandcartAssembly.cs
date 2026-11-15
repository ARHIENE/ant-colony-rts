using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    // Start runs after the construction site finishes consuming and releasing its materials.
    public sealed class HandcartAssembly : BuildingBase
    {
        private void Start()
        {
            var world = WorldMapManager.Instance;
            if (world == null) return;
            var ground = Position;
            if (UnityEngine.AI.NavMesh.SamplePosition(Position, out var hit, 2, UnityEngine.AI.NavMesh.AllAreas)) ground = hit.position;
            else if (GetComponent<Collider>() is Collider footprint) ground.y = footprint.bounds.min.y;
            world.CreateTransport(false, ground, handcart: true);
            Destroy(gameObject);
        }
    }
}

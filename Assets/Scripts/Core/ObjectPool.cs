using System.Collections.Generic;
using UnityEngine;

namespace AntColony.Core
{
    public class ObjectPool : MonoBehaviour
    {
        private readonly Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
        private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new Dictionary<GameObject, GameObject>();

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!pools.TryGetValue(prefab, out var queue))
            {
                queue = new Queue<GameObject>();
                pools[prefab] = queue;
            }

            GameObject instance;
            if (queue.Count > 0)
            {
                instance = queue.Dequeue();
                instance.transform.SetPositionAndRotation(position, rotation);
                instance.SetActive(true);
            }
            else
            {
                instance = Instantiate(prefab, position, rotation);
                instanceToPrefab[instance] = prefab;
            }

            return instance;
        }

        public void Release(GameObject instance)
        {
            if (!instanceToPrefab.TryGetValue(instance, out var prefab))
            {
                Destroy(instance);
                return;
            }

            instance.SetActive(false);
            pools[prefab].Enqueue(instance);
        }
    }
}

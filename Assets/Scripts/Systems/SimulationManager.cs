using UnityEngine;
using System.Collections.Generic;

namespace DroneSim
{
    public class SimulationManager : MonoBehaviour
    {
        public static SimulationManager Instance { get; private set; }

        [Header("Scene refs")]
        public ResourceSpawner Spawner;
        public DroneAgent dronePrefab;
        public BaseHub baseA;
        public BaseHub baseB;

        [Header("Config")]
        [Range(1, 10)] public int dronesPerBase = 3;
        [Range(1, 10)] public float droneSpeed = 4;

        readonly List<DroneAgent> _drones = new();
        public IReadOnlyList<DroneAgent> Drones => _drones;

        void Awake()
        {
            Instance = this;
            SpawnBatch(baseA, dronesPerBase);
            SpawnBatch(baseB, dronesPerBase);
        }

        void SpawnBatch(BaseHub hub, int n)
        {
            for (int i = 0; i < n; i++)
            {
                var d = Instantiate(dronePrefab, hub.transform.position, Quaternion.identity);
                d.Init(hub, droneSpeed);
                _drones.Add(d);
            }
        }

        // ----- UI hooks -----
        public void SetSpeed(float v)
        {
            droneSpeed = v;
            _drones.ForEach(d => d.GetComponent<UnityEngine.AI.NavMeshAgent>().speed = v);
        }

        public void SetAmount(int perBase)
        {
            perBase = Mathf.Clamp(perBase, 1, 10);
            if (perBase == dronesPerBase) return;
            dronesPerBase = perBase;
            Adjust(baseA, perBase); Adjust(baseB, perBase);
        }

        void Adjust(BaseHub hub, int target)
        {
            var list = _drones.FindAll(d => d.Home == hub);
            if (list.Count < target)
                SpawnBatch(hub, target - list.Count);
            else
            {
                int rem = list.Count - target;
                for (int i = 0; i < rem; i++)
                {
                    var d = list[i];
                    _drones.Remove(d);
                    Destroy(d.gameObject);
                }
            }
        }
    }
}

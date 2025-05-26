using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace DroneSim
{
    public class ResourceSpawner : MonoBehaviour
    {
        [SerializeField] ResourceNode prefab;
        [SerializeField] Vector2 area = new(10, 10);
        [SerializeField, Range(.2f, 10)] float interval = 3f;
        [SerializeField, Range(1, 50)] int maxNodes = 25;

        readonly List<ResourceNode> _pool = new();
        public IReadOnlyList<ResourceNode> Pool => _pool;

        void Start() => InvokeRepeating(nameof(Spawn), 0, interval);

        public void SetInterval(float sec)
        {
            interval = Mathf.Max(.2f, sec);
            CancelInvoke();
            InvokeRepeating(nameof(Spawn), 0, interval);
        }

        void Spawn()
        {
            _pool.RemoveAll(n => n == null || !n.gameObject.activeSelf);
            if (_pool.Count >= maxNodes) return;

            Vector3 rnd = new(Random.Range(-area.x, area.x), 0,
                              Random.Range(-area.y, area.y));

            if (NavMesh.SamplePosition(rnd, out var hit, 2f, NavMesh.AllAreas))
                _pool.Add(Instantiate(prefab, hit.position, Quaternion.identity, transform));
        }

        public ResourceNode GetNearestFree(Vector3 pos)
        {
            float best = float.MaxValue;
            ResourceNode bestNode = null;

            foreach (var n in _pool)
            {
                if (!n || !n.IsFree) continue;
                float d = (n.transform.position - pos).sqrMagnitude;
                if (d < best && n.TryReserve()) { best = d; bestNode = n; }
            }
            return bestNode;
        }
    }
}

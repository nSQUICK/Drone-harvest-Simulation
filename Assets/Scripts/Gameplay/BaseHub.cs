using UnityEngine;
using UnityEngine.AI;

namespace DroneSim
{
    [RequireComponent(typeof(Renderer))]
    public class BaseHub : MonoBehaviour
    {
        [SerializeField] GameObject slotMarkerPrefab;
        GameObject[] _markers;
        [SerializeField] string faction = "Blue";
        [SerializeField] Color color = Color.cyan;

        [Header("Unload circle")]
        [Range(1f, 4f)] public float radius = 1.5f;
        [Range(4, 16)] public int slots = 8;

        [SerializeField] float slotTimeout = 3f;     // сек
        float[] _reserveTime;
        bool[] _busy;
        int _store;

        void Awake()
        {
            _reserveTime = new float[slots];
            _busy = new bool[slots];
            var r = GetComponent<Renderer>();
            r.material = new Material(r.sharedMaterial) { color = color };
            if (slotMarkerPrefab)
            {
                _markers = new GameObject[slots];
                for (int i = 0; i < slots; i++)
                {
                    Vector3 p = transform.position +
                        new Vector3(Mathf.Cos(i * 2 * Mathf.PI / slots), 0,
                                    Mathf.Sin(i * 2 * Mathf.PI / slots)) * radius;
                    _markers[i] = Instantiate(slotMarkerPrefab, p + Vector3.up * 0.05f,
                                              Quaternion.identity, transform);
                    SetMarker(i, false); // свободен
                }
            }
        }
        void SetMarker(int i, bool busy)
        {
            if (_markers == null) return;
            _markers[i].GetComponent<Renderer>().material.color =
                busy ? Color.red : new Color(color.r, color.g, color.b, 0.3f);
        }

        // ── slot logic ─────────────────────────────
        public bool TryGetSlot(out Vector3 pos, out int id)
        {
            for (int i = 0; i < slots; i++)
                if (!_busy[i])
                {
                    Reserve(i);
                    // ★ правка — заполняем out-параметры, а возвращаем TRUE
                    SlotPos(i, out pos, out id);
                    return true;
                }

            // все места заняты — дрону даём центр базы
            pos = transform.position;
            id = -1;
            return false;
        }

        public void FreeSlot(int id) { if (id >= 0) _busy[id] = false; }


        Vector3 SlotPos(int i, out Vector3 p, out int id)
        {
            float ang = 2 * Mathf.PI * i / slots;
            Vector3 raw = transform.position +
                          new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * radius;

            // Новое: ищем ближайшую точку NavMesh в 0.5 м
            if (NavMesh.SamplePosition(raw, out var hit, 0.5f, NavMesh.AllAreas))
                p = hit.position;
            else
                p = raw;                           // fallback (почти не бывает)

            id = i;
            return p;
        }
        void LateUpdate()   // каждую рамку чистим “мёртвые” брони
        {
            for (int i = 0; i < slots; i++)
                if (_busy[i] && Time.time - _reserveTime[i] > slotTimeout)
                    _busy[i] = false;          // слот давно никто не занял → освобождаем
        }
        void Reserve(int i)
        {
            _busy[i] = true;
            _reserveTime[i] = Time.time;
        }

        // ── store & getters ────────────────────────
        public void AddResource() => _store++;

        public int Store => _store;
        public string Faction => faction;
        public Color Color => color;
    }
}

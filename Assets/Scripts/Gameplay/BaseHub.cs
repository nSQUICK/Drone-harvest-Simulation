using UnityEngine;
using UnityEngine.Events;

namespace DroneSim
{
    [RequireComponent(typeof(Renderer))]
    public class BaseHub : MonoBehaviour
    {
        [SerializeField] string faction = "Blue";
        [SerializeField] Color color = Color.cyan;
        [Header("Unload circle")]
        [Range(1f, 4f)] public float radius = 2f;
        [Range(4, 12)] public int slots = 8;

        public UnityEvent OnResourceGet;

        bool[] _busy;     
        int _store;

        void Awake()
        {
            _busy = new bool[slots];
            var r = GetComponent<Renderer>();
            r.material = new Material(r.sharedMaterial) { color = color };
        }

        // -------- queue ------------
        public bool TryGetSlot(out Vector3 point, out int id)
        {
            for (int i = 0; i < slots; i++)
                if (!_busy[i])
                {
                    Reserve(i);
                    SlotPos(i, out point, out id);  
                    return true;                     
                }

            // Все заняты — возвращаем центр
            point = transform.position;
            id = -1;
            return false;
        }
        public void FreeSlot(int id) { if (id >= 0) _busy[id] = false; }

        void Reserve(int i) => _busy[i] = true;

        Vector3 SlotPos(int i, out Vector3 p, out int id)
        {
            float ang = 2 * Mathf.PI * i / slots;
            p = transform.position + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * radius;
            id = i;
            return p;
        }

        // -------- store ------------
        public void AddResource() { _store++;
            OnResourceGet.Invoke();
        }
        public int Store => _store;
        public string Faction => faction;
        public Color Color => color;
    }
}

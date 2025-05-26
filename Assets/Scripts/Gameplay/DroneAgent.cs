using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace DroneSim
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class DroneAgent : MonoBehaviour
    {
        enum St { Idle, Seek, Harvest, Return }

        [SerializeField] float harvestTime = 2f;

        NavMeshAgent _ag;
        BaseHub _home;
        ResourceNode _target;
        int _slotId = -1;
        bool _carry;
        St _state;

        LineRenderer _lr;
        static bool _draw;

        const float SepRad = 1.25f, SepForce = 3f;

        void Awake()
        {
            _ag = GetComponent<NavMeshAgent>();
            _ag.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            _ag.radius *= .8f;
            _ag.angularSpeed = 720;
            _ag.acceleration = 1f;
            _ag.autoBraking = true;

            gameObject.layer = LayerMask.NameToLayer("Drone");

            _lr = gameObject.AddComponent<LineRenderer>();
            _lr.material = new Material(Shader.Find("Sprites/Default"));
            _lr.widthMultiplier = .05f;
            _lr.enabled = _draw;
            
        }

        public void Init(BaseHub hub, float speed)
        {
            _home = hub;
            _ag.speed = speed;
            _lr.material.color = hub.Color;
            _lr.startWidth = 1f;

            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Color", hub.Color);
            GetComponent<Renderer>().SetPropertyBlock(mpb);

            NextTarget();
            StartCoroutine(RepathLoop());
            StartCoroutine(IdleLoop());
            StartCoroutine(StuckGuard());
        }

        void Update()
        {
            switch (_state)
            {
                case St.Seek:
                    if (!_target) { NextTarget(); break; }
                    if (!_ag.pathPending && _ag.remainingDistance < .15f)
                        StartCoroutine(Harvest());
                    break;

                case St.Return:
                    float r = _home.radius + _ag.stoppingDistance;
                    if ((transform.position - _home.transform.position).sqrMagnitude <= r * r && _carry)
                    {
                        _home.AddResource();
                        _home.FreeSlot(_slotId);
                        _slotId = -1; _carry = false;
                        NextTarget();
                    }
                    break;
            }

            Separation();
            DrawPath();
        }
        IEnumerator IdleLoop()
        {
            var wait = new WaitForSeconds(0.5f);
            while (true)
            {
                if (_state == St.Idle) NextTarget();
                yield return wait;
            }
        }

        IEnumerator Harvest()
        {
            _state = St.Harvest;
            yield return new WaitForSeconds(harvestTime);
            if (_home.TryGetSlot(out var p, out _slotId) == false)
            {
                // fallback — случайная точка по окружности, но не центр
                float ang = Random.value * Mathf.PI * 2f;
                p = _home.transform.position +
                    new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * (_home.radius * 0.9f);
            }
            _ag.SetDestination(p);
            _state = St.Return;

        }

        public void NextTarget()
        {
            _target = SimulationManager.Instance.Spawner.GetNearestFree(transform.position);
            if (_target)
            {
                _ag.SetDestination(_target.transform.position);
                _state = St.Seek;
            }
            else
                _state = St.Idle;
        }

        // ── helpers ───────────────────────────────
        void Separation()
        {
            var hits = Physics.OverlapSphere(transform.position, SepRad,
                       LayerMask.GetMask("Drone"));
            Vector3 steer = Vector3.zero; int n = 0;
            foreach (var h in hits)
            {
                if (h.gameObject == gameObject) continue;
                var diff = transform.position - h.transform.position;
                steer += diff / (diff.sqrMagnitude + .01f); n++;
            }
            if (n > 0) _ag.Move(steer.normalized * SepForce * Time.deltaTime);
        }

        void DrawPath()
        {
            if (!_lr.enabled || !_ag.hasPath) return;
            var c = _ag.path.corners;
            _lr.positionCount = c.Length;
            for (int i = 0; i < c.Length; i++)
                _lr.SetPosition(i, c[i] + Vector3.up * .04f);
        }

        IEnumerator RepathLoop()
        {
            var w = new WaitForSeconds(.5f);
            while (true)
            {
                if (_ag.hasPath) _ag.SetDestination(_ag.destination);
                yield return w;
            }
        }

        IEnumerator StuckGuard()
        {
            var wait = new WaitForSeconds(2f);
            Vector3 lastPos = transform.position;

            while (true)
            {
                yield return wait;

                // если почти не двигался 2 с, но не Idle → репатч
                if (_state != St.Idle &&
                    (transform.position - lastPos).sqrMagnitude < 0.01f)
                {
                    _ag.ResetPath();
                    NextTarget();               // берём новый ресурс/слот
                }
                lastPos = transform.position;
            }
        }

        // ── static API ────────────────────────────
        public static void ShowPaths(bool on)
        {
            _draw = on;
            foreach (var d in SimulationManager.Instance.Drones)
                d._lr.enabled = on;
        }

        // ── getter для SimulationManager ──────────
        public BaseHub Home => _home;
    }
}

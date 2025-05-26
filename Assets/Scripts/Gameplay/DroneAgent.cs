using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace DroneSim
{
    [RequireComponent(typeof(NavMeshAgent))]
    [AddComponentMenu("DroneSim/Drone Agent")]
    public class DroneAgent : MonoBehaviour
    {
        enum St { Idle, Seek, Harvest, Return }
        St _state;

        [SerializeField] float harvestTime = 2f;

        NavMeshAgent _agent;
        BaseHub _home;
        ResourceNode _target;
        int _slotId = -1;
        bool _carrying;

        LineRenderer _lr;
        static bool _draw;

        const float SepRad = 1.2f;
        const float SepForce = 2f;
        public BaseHub Home => _home;
        // ---------- init ----------
        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            _agent.radius *= .8f;
            _agent.angularSpeed = 720;
            _agent.acceleration *= 2.5f;
            _agent.autoBraking = true;
            _agent.stoppingDistance = 0.35f;

            gameObject.layer = LayerMask.NameToLayer("Drone");

            _lr = gameObject.AddComponent<LineRenderer>();
            _lr.material = new Material(Shader.Find("Sprites/Default"));
            _lr.widthMultiplier = .05f;
            _lr.enabled = _draw;
        }

        public void Init(BaseHub hub, float speed)
        {
            _home = hub;
            _agent.speed = speed;

            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Color", hub.Color);
            GetComponent<Renderer>().SetPropertyBlock(mpb);
            StartCoroutine(IdleLoop());

            NextTarget();
            StartCoroutine(Repath());
        }
        IEnumerator IdleLoop()
        {
            var wait = new WaitForSeconds(1f);
            while (true)
            {
                if (_state == St.Idle) NextTarget();
                yield return wait;
            }
        }
        // ---------- main ----------
        void Update()
        {
            switch (_state)
            {
                case St.Seek:
                    if (!_target) { NextTarget(); break; }

                    if (!_agent.pathPending && _agent.remainingDistance < .15f)
                        StartCoroutine(Harvest());
                    break;

                case St.Return:
                    float r = _home.radius + _agent.stoppingDistance;
                    if ((transform.position - _home.transform.position).sqrMagnitude <= r * r && _carrying)
                    {
                        _home.AddResource();
                        _home.FreeSlot(_slotId);
                        _slotId = -1; _carrying = false;
                        NextTarget();
                    }
                    break;
            }

            Separation();
            DrawPath();
        }

        IEnumerator Harvest()
        {
            _state = St.Harvest;
            yield return new WaitForSeconds(harvestTime);

            if (_target && _target.TryTake())
            {
                _target.gameObject.SetActive(false);
                _carrying = true;
            }

            _home.TryGetSlot(out var p, out _slotId);
            _agent.SetDestination(p);
            _state = St.Return;
        }

        public void NextTarget()
        {
            _target = SimulationManager.Instance.Spawner.GetNearestFree(transform.position);
            if (_target)
            {
                _agent.SetDestination(_target.transform.position);
                _state = St.Seek;
            }
            else
                _state = St.Idle;
        }

        // ---------- helpers ----------
        void Separation()
        {
            var hits = Physics.OverlapSphere(transform.position, SepRad, LayerMask.GetMask("Drone"));
            Vector3 steer = Vector3.zero; int n = 0;
            foreach (var h in hits)
            {
                if (h.gameObject == gameObject) continue;
                var diff = transform.position - h.transform.position;
                steer += diff / (diff.sqrMagnitude + .01f); n++;
            }
            if (n > 0) _agent.Move(steer.normalized * SepForce * Time.deltaTime);
        }

        void DrawPath()
        {
            if (!_lr.enabled || !_agent.hasPath) return;
            var c = _agent.path.corners;
            _lr.positionCount = c.Length;
            for (int i = 0; i < c.Length; i++)
                _lr.SetPosition(i, c[i] + Vector3.up * .04f);
        }

        IEnumerator Repath()
        {
            var wait = new WaitForSeconds(.5f);
            while (true)
            {
                if (_agent.hasPath) _agent.SetDestination(_agent.destination);
                yield return wait;
            }
        }

        // ---------- static ----------
        public static void ShowPaths(bool on)
        {
            _draw = on;
            foreach (var d in SimulationManager.Instance.Drones) d._lr.enabled = on;
        }
    }
}

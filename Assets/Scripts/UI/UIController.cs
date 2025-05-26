using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DroneSim
{
    public class UIController : MonoBehaviour
    {
        [SerializeField] Slider speedSl;
        [SerializeField] Slider amountSl;
        [SerializeField] TMP_InputField spawnIn;
        [SerializeField] Toggle pathTg;

        [SerializeField] TextMeshProUGUI txtA;
        [SerializeField] TextMeshProUGUI txtB;
        [SerializeField] BaseHub hubA;
        [SerializeField] BaseHub hubB;

        void Start()
        {
            speedSl.onValueChanged.AddListener(v => SimulationManager.Instance.SetSpeed(v));
            amountSl.onValueChanged.AddListener(v => SimulationManager.Instance.SetAmount(Mathf.RoundToInt(v)));
            spawnIn.onEndEdit.AddListener(s => {
                if (float.TryParse(s, out var f))
                    SimulationManager.Instance.Spawner.SetInterval(f);
            });
            pathTg.onValueChanged.AddListener(DroneAgent.ShowPaths);
        }

        void Update()
        {
            txtA.text = $"{hubA.Faction}: {hubA.Store}";
            txtB.text = $"{hubB.Faction}: {hubB.Store}";
        }
    }
}

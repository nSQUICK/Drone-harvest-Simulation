using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DroneSim
{
    public class UIController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] Slider speedSlider;
        [SerializeField] Slider amountSlider;
        [SerializeField] TMP_InputField spawnInput;
        [SerializeField] Toggle pathToggle;

        [Header("Counters")]
        [SerializeField] TextMeshProUGUI txtA;
        [SerializeField] TextMeshProUGUI txtB;
        [SerializeField] BaseHub hubA;
        [SerializeField] BaseHub hubB;

        void Start()
        {
            speedSlider.onValueChanged.AddListener(v => SimulationManager.Instance.SetSpeed(v));
            amountSlider.onValueChanged.AddListener(v => SimulationManager.Instance.SetAmount(Mathf.RoundToInt(v)));
            spawnInput.onEndEdit.AddListener(s =>
            {
                if (float.TryParse(s, out var f))
                    SimulationManager.Instance.Spawner.SetInterval(f);
            });
            pathToggle.onValueChanged.AddListener(DroneAgent.ShowPaths);
        }

        void Update()
        {
            txtA.text = $"{hubA.Faction}: {hubA.Store}";
            txtB.text = $"{hubB.Faction}: {hubB.Store}";
        }
    }
}

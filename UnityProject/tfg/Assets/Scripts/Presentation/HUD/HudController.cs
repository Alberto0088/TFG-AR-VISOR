using TMPro;
using UnityEngine;

namespace TFG.ARVisor.Presentation.HUD
{
    public class HudController : MonoBehaviour
    {
        [Header("HUD Text References")]
        [SerializeField] private TMP_Text systemText;
        [SerializeField] private TMP_Text trafficText;
        [SerializeField] private TMP_Text alertText;
        [SerializeField] private TMP_Text reticleText;

        [Header("Simulation")]
        [SerializeField] private bool useSimulation = true;
        [SerializeField] private float simulationRefreshSeconds = 2f;

        private float timer;
        private int simulationStep;

        private void Start()
        {
            RenderDefaultHud();
        }

        private void Update()
        {
            if (!useSimulation)
            {
                return;
            }

            timer += Time.deltaTime;

            if (timer >= simulationRefreshSeconds)
            {
                timer = 0f;
                UpdateSimulation();
            }
        }

        private void RenderDefaultHud()
        {
            UpdateSystemPanel("ONLINE", "SIM", "3D", "1 Hz");
            UpdateTrafficPanel(0, "--", "LOW");
            UpdateAlertPanel("NO ALERTS", RiskLevel.Low);

            if (reticleText != null)
            {
                reticleText.text = "+";
            }
        }

        public void UpdateSystemPanel(string status, string gpsStatus, string mode, string updateRate)
        {
            if (systemText == null)
            {
                return;
            }

            systemText.text =
                $"SYS  {status}\n" +
                $"GPS  {gpsStatus}\n" +
                $"MODE {mode}\n" +
                $"UPD  {updateRate}";
        }

        public void UpdateTrafficPanel(int nearbyAircraft, string nearestDistance, string risk)
        {
            if (trafficText == null)
            {
                return;
            }

            trafficText.text =
                $"TRAF {nearbyAircraft}\n" +
                $"NEAR {nearestDistance}\n" +
                $"RISK {risk}";
        }

        public void UpdateAlertPanel(string message, RiskLevel riskLevel)
        {
            if (alertText == null)
            {
                return;
            }

            alertText.text = message;

            switch (riskLevel)
            {
                case RiskLevel.Low:
                    alertText.color = new Color(0.85f, 0.85f, 0.85f);
                    break;

                case RiskLevel.Medium:
                    alertText.color = new Color(1f, 0.8f, 0.2f);
                    break;

                case RiskLevel.High:
                    alertText.color = new Color(1f, 0.2f, 0.2f);
                    break;
            }
        }

        private void UpdateSimulation()
        {
            simulationStep++;

            switch (simulationStep % 4)
            {
                case 0:
                    UpdateSystemPanel("ONLINE", "SIM", "3D", "1 Hz");
                    UpdateTrafficPanel(0, "--", "LOW");
                    UpdateAlertPanel("NO ALERTS", RiskLevel.Low);
                    break;

                case 1:
                    UpdateSystemPanel("ONLINE", "SIM", "3D", "1 Hz");
                    UpdateTrafficPanel(2, "8.4 KM", "LOW");
                    UpdateAlertPanel("NO ALERTS", RiskLevel.Low);
                    break;

                case 2:
                    UpdateSystemPanel("ONLINE", "SIM", "3D", "1 Hz");
                    UpdateTrafficPanel(3, "3.1 KM", "MED");
                    UpdateAlertPanel("TRAFFIC ADVISORY", RiskLevel.Medium);
                    break;

                case 3:
                    UpdateSystemPanel("ONLINE", "SIM", "3D", "1 Hz");
                    UpdateTrafficPanel(1, "1.2 KM", "HIGH");
                    UpdateAlertPanel("COLLISION RISK", RiskLevel.High);
                    break;
            }
        }
    }

    public enum RiskLevel
    {
        Low,
        Medium,
        High
    }
}
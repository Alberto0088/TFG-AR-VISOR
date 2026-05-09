using TFG.ARVisor.Domain.Models;
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

        private void Start()
        {
            RenderSystemStatus("ONLINE", "SIM", "3D", "1 Hz");
            RenderTraffic(new TrafficSnapshot(0, "--", RiskLevel.Low, "NO ALERTS"));

            if (reticleText != null)
            {
                reticleText.text = "+";
            }
        }

        public void RenderSystemStatus(string status, string gpsStatus, string mode, string updateRate)
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

        public void RenderTraffic(TrafficSnapshot snapshot)
        {
            if (trafficText != null)
            {
                trafficText.text =
                    $"TRAF {snapshot.NearbyAircraft}\n" +
                    $"NEAR {snapshot.NearestDistance}\n" +
                    $"RISK {FormatRisk(snapshot.RiskLevel)}";
            }

            RenderAlert(snapshot.AlertMessage, snapshot.RiskLevel);
        }

        private void RenderAlert(string message, RiskLevel riskLevel)
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

        private string FormatRisk(RiskLevel riskLevel)
        {
            switch (riskLevel)
            {
                case RiskLevel.Low:
                    return "LOW";

                case RiskLevel.Medium:
                    return "MED";

                case RiskLevel.High:
                    return "HIGH";

                default:
                    return "--";
            }
        }
    }
}
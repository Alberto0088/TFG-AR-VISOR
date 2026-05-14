/*
 * HudController.cs
 * ------------------------------------------------------------
 * Este script controla los textos principales del HUD del visor.
 *
 * Su función es recibir datos ya procesados por otros módulos y mostrarlos
 * en pantalla de forma clara:
 * - estado del sistema,
 * - estado del GPS,
 * - tráfico aéreo cercano,
 * - aeronave más relevante,
 * - nivel de riesgo,
 * - mensaje de alerta,
 * - retícula central.
 *
 * Se conecta con:
 * - ExternalGpsProvider: actualiza el panel de sistema con GPS REAL / WAIT.
 * - OpenSkyApiClient: actualiza el panel de tráfico con datos de OpenSky.
 * - TrafficSnapshot: modelo que contiene la información que se debe mostrar.
 */

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

        /// <summary>
        /// Inicializa el HUD con valores por defecto para que no aparezcan textos vacíos al arrancar la escena.
        /// </summary>
        private void Start()
        {
            RenderSystemStatus("ONLINE", "SIM", "3D", "1 Hz");
            RenderTraffic(new TrafficSnapshot(0, "--", RiskLevel.Low, "NO ALERTS"));

            if (reticleText != null)
            {
                reticleText.text = "+";
            }
        }

        /// <summary>
        /// Actualiza el panel izquierdo del HUD con el estado general del sistema y del GPS.
        /// </summary>
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

        /// <summary>
        /// Actualiza el panel derecho del HUD con el resumen de tráfico, la aeronave relevante
        /// y una diferenciación visual según el nivel de riesgo calculado.
        /// </summary>
        public void RenderTraffic(TrafficSnapshot snapshot)
        {
            if (trafficText != null)
            {
                trafficText.text = BuildTrafficText(snapshot);
                trafficText.color = GetRiskColor(snapshot != null ? snapshot.RiskLevel : RiskLevel.Low);
            }

            if (snapshot != null)
            {
                RenderAlert(snapshot.AlertMessage, snapshot.RiskLevel);
            }
        }

        /// <summary>
        /// Construye el texto del panel derecho del HUD.
        /// Destaca la aeronave relevante y adapta el texto al nivel de riesgo.
        /// </summary>
        private string BuildTrafficText(TrafficSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return
                    "TRAFFIC\n" +
                    "NO DATA\n\n" +
                    "TRAF  --\n" +
                    "NEAR  --\n" +
                    "RISK  --";
            }

            string riskLabel = GetRiskLabel(snapshot.RiskLevel);

            if (HasRelevantAircraft(snapshot))
            {
                return
                    "TARGET\n" +
                    $"{snapshot.RelevantCallsign}\n\n" +
                    $"DIST  {snapshot.NearestDistance}\n" +
                    $"ALT   {snapshot.RelevantAltitude}\n" +
                    $"HDG   {snapshot.RelevantHeading}\n\n" +
                    $"TRAF  {snapshot.NearbyAircraft}\n" +
                    $"RISK  {riskLabel}";
            }

            return
                "TRAFFIC\n" +
                "NO TARGET\n\n" +
                $"TRAF  {snapshot.NearbyAircraft}\n" +
                $"NEAR  {snapshot.NearestDistance}\n" +
                $"RISK  {riskLabel}";
        }

        /// <summary>
        /// Comprueba si el snapshot contiene datos suficientes para mostrar una aeronave destacada.
        /// </summary>
        private bool HasRelevantAircraft(TrafficSnapshot snapshot)
        {
            return snapshot != null &&
                   !string.IsNullOrWhiteSpace(snapshot.RelevantCallsign);
        }

        /// <summary>
        /// Actualiza el mensaje superior de alerta y lo hace más visible según el nivel de riesgo.
        /// </summary>
        private void RenderAlert(string message, RiskLevel riskLevel)
        {
            if (alertText == null)
            {
                return;
            }

            switch (riskLevel)
            {
                case RiskLevel.High:
                    alertText.text = $"!!! {message} !!!";
                    break;

                case RiskLevel.Medium:
                    alertText.text = $"-- {message} --";
                    break;

                default:
                    alertText.text = message;
                    break;
            }

            alertText.color = GetRiskColor(riskLevel);
        }

        /// <summary>
        /// Devuelve el color visual asociado a cada nivel de riesgo.
        /// </summary>
        private Color GetRiskColor(RiskLevel riskLevel)
        {
            switch (riskLevel)
            {
                case RiskLevel.High:
                    return new Color(1f, 0.2f, 0.2f);

                case RiskLevel.Medium:
                    return new Color(1f, 0.8f, 0.2f);

                default:
                    return new Color(0.9f, 0.9f, 0.9f);
            }
        }

        /// <summary>
        /// Devuelve el texto corto que representa el nivel de riesgo en el HUD.
        /// </summary>
        private string GetRiskLabel(RiskLevel riskLevel)
        {
            switch (riskLevel)
            {
                case RiskLevel.High:
                    return "HIGH";

                case RiskLevel.Medium:
                    return "MED";

                default:
                    return "LOW";
            }
        }

        /// <summary>
        /// Convierte el nivel de riesgo interno en texto corto para el HUD.
        /// </summary>
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
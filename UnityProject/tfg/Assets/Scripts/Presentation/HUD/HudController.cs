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
        /// Actualiza el panel derecho del HUD con el resumen de tráfico y la aeronave más relevante.
        /// </summary>
        public void RenderTraffic(TrafficSnapshot snapshot)
        {
            if (trafficText != null)
            {
                trafficText.text = BuildTrafficText(snapshot);
            }

            RenderAlert(snapshot.AlertMessage, snapshot.RiskLevel);
        }

        /// <summary>
        /// Construye el texto del panel derecho dependiendo de si existe o no una aeronave relevante.
        /// </summary>
        /// <summary>
        /// Construye el texto del panel derecho del HUD.
        /// Si existe una aeronave relevante, muestra sus datos de forma prioritaria.
        /// Si no existe, muestra un estado limpio de ausencia de tráfico relevante.
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

            if (HasRelevantAircraft(snapshot))
            {
                return
                    "TARGET\n" +
                    $"{snapshot.RelevantCallsign}\n\n" +
                    $"DIST  {snapshot.NearestDistance}\n" +
                    $"ALT   {snapshot.RelevantAltitude}\n" +
                    $"HDG   {snapshot.RelevantHeading}\n\n" +
                    $"TRAF  {snapshot.NearbyAircraft}\n" +
                    $"RISK  {FormatRisk(snapshot.RiskLevel)}";
            }

            return
                "TRAFFIC\n" +
                "NO TARGET\n\n" +
                $"TRAF  {snapshot.NearbyAircraft}\n" +
                $"NEAR  {snapshot.NearestDistance}\n" +
                $"RISK  {FormatRisk(snapshot.RiskLevel)}";
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
        /// Actualiza el mensaje superior de alerta y cambia su color según el nivel de riesgo.
        /// </summary>
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
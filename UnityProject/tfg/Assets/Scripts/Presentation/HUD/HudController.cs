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
 * - aeronave seleccionada como TARGET,
 * - sector de mirada del piloto,
 * - sector relativo de la aeronave,
 * - mini radar de cabina,
 * - nivel de riesgo,
 * - mensaje superior de alerta,
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
        /// Inicializa el HUD con valores por defecto para evitar textos vacíos al arrancar la escena.
        /// </summary>
        private void Start()
        {
            EnableRichText();
            RenderSystemStatus("ONLINE", "SIM", "3D", "1 Hz");
            RenderTraffic(new TrafficSnapshot(0, "--", RiskLevel.Low, "NO ALERTS"));

            if (reticleText != null)
            {
                reticleText.text = "+";
            }
        }

        /// <summary>
        /// Activa Rich Text en los TextMeshPro para poder usar color y tamaño dentro del texto.
        /// </summary>
        private void EnableRichText()
        {
            if (systemText != null)
            {
                systemText.richText = true;
            }

            if (trafficText != null)
            {
                trafficText.richText = true;
            }

            if (alertText != null)
            {
                alertText.richText = true;
            }

            if (reticleText != null)
            {
                reticleText.richText = true;
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
                "<size=78%>SYSTEM</size>\n" +
                $"SYS  {status}\n" +
                $"GPS  {gpsStatus}\n" +
                $"MODE {mode}\n" +
                $"UPD  {updateRate}";
        }

        /// <summary>
        /// Actualiza el panel derecho del HUD con el TARGET, la zona de mirada, el radar y el riesgo.
        /// </summary>
        public void RenderTraffic(TrafficSnapshot snapshot)
        {
            if (trafficText != null)
            {
                trafficText.text = BuildTrafficText(snapshot);
                trafficText.color = new Color(0.9f, 0.9f, 0.9f);
            }

            UpdateReticle(snapshot);

            if (snapshot != null)
            {
                RenderAlert(snapshot.AlertMessage, snapshot.RiskLevel);
            }
        }

        /// <summary>
        /// Construye el texto del panel derecho con un formato compacto y un mini radar de cabina.
        /// </summary>
        private string BuildTrafficText(TrafficSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return
                    "<size=78%>CABIN RADAR</size>\n" +
                    BuildCabinRadar(null) + "\n\n" +
                    "<size=78%>VIEW --</size>\n" +
                    "<size=120%>NO DATA</size>\n\n" +
                    "TRAF --\n" +
                    "RISK --";
            }

            string riskLabel = GetRiskLabel(snapshot.RiskLevel);
            string riskColor = GetRiskHexColor(snapshot.RiskLevel);
            string viewSector = ExtractSectorName(snapshot.ViewSector);
            string targetSector = ExtractSectorName(snapshot.TargetSector);
            string selectionMode = FormatSelectionMode(snapshot.SelectionMode);

            if (HasRelevantAircraft(snapshot))
            {
                return
                    "<size=78%>CABIN RADAR</size>\n" +
                    BuildCabinRadar(snapshot) + "\n\n" +
                    $"<size=72%>VIEW {viewSector} · {selectionMode}</size>\n" +
                    $"<size=125%>{snapshot.RelevantCallsign}</size>\n" +
                    $"<size=82%>{targetSector} · {snapshot.NearestDistance}</size>\n\n" +
                    $"ALT  {snapshot.RelevantAltitude}\n" +
                    $"HDG  {snapshot.RelevantHeading}\n" +
                    $"TRAF {snapshot.NearbyAircraft}\n" +
                    $"RISK <color={riskColor}>{riskLabel}</color>";
            }

            return
                "<size=78%>CABIN RADAR</size>\n" +
                BuildCabinRadar(snapshot) + "\n\n" +
                $"<size=72%>VIEW {viewSector}</size>\n" +
                "<size=115%>NO TARGET</size>\n\n" +
                $"TRAF {snapshot.NearbyAircraft}\n" +
                $"NEAR {snapshot.NearestDistance}\n" +
                $"RISK <color={riskColor}>{riskLabel}</color>";
        }

        /// <summary>
        /// Construye un mini radar textual de cabina basado en el sector donde se encuentra el target.
        /// </summary>
        private string BuildCabinRadar(TrafficSnapshot snapshot)
        {
            string targetSector = snapshot != null
                ? ExtractSectorName(snapshot.TargetSector)
                : "--";

            RiskLevel riskLevel = snapshot != null
                ? snapshot.RiskLevel
                : RiskLevel.Low;

            string front = BuildRadarSector("F", "FRONT", targetSector, riskLevel);
            string left = BuildRadarSector("L", "LEFT", targetSector, riskLevel);
            string right = BuildRadarSector("R", "RIGHT", targetSector, riskLevel);
            string rear = BuildRadarSector("B", "REAR", targetSector, riskLevel);

            return
                $"      {front}\n" +
                $"  {left}   +   {right}\n" +
                $"      {rear}";
        }

        /// <summary>
        /// Devuelve una celda del radar destacando el sector en el que está el target.
        /// </summary>
        private string BuildRadarSector(
            string shortLabel,
            string sectorName,
            string targetSector,
            RiskLevel riskLevel)
        {
            if (targetSector == sectorName)
            {
                return $"<color={GetRiskHexColor(riskLevel)}>[{shortLabel}]</color>";
            }

            return $" {shortLabel} ";
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
        /// Extrae el nombre del sector desde textos como VIEW LEFT o SECTOR RIGHT.
        /// </summary>
        private string ExtractSectorName(string sectorText)
        {
            if (string.IsNullOrWhiteSpace(sectorText))
            {
                return "--";
            }

            string upper = sectorText.ToUpperInvariant();

            if (upper.Contains("FRONT"))
            {
                return "FRONT";
            }

            if (upper.Contains("RIGHT"))
            {
                return "RIGHT";
            }

            if (upper.Contains("LEFT"))
            {
                return "LEFT";
            }

            if (upper.Contains("REAR"))
            {
                return "REAR";
            }

            return "--";
        }

        /// <summary>
        /// Simplifica el modo de selección para mostrarlo de forma compacta en el HUD.
        /// </summary>
        private string FormatSelectionMode(string selectionMode)
        {
            if (string.IsNullOrWhiteSpace(selectionMode))
            {
                return "NO LOCK";
            }

            string upper = selectionMode.ToUpperInvariant();

            if (upper.Contains("VIEW"))
            {
                return "VIEW LOCK";
            }

            if (upper.Contains("NEAREST"))
            {
                return "NEAREST";
            }

            return selectionMode;
        }

        /// <summary>
        /// Actualiza la retícula central para que indique visualmente si hay target.
        /// </summary>
        private void UpdateReticle(TrafficSnapshot snapshot)
        {
            if (reticleText == null)
            {
                return;
            }

            if (snapshot != null && HasRelevantAircraft(snapshot))
            {
                reticleText.text = "[ + ]";
                reticleText.color = GetRiskColor(snapshot.RiskLevel);
                return;
            }

            reticleText.text = "+";
            reticleText.color = new Color(0.85f, 0.85f, 0.85f);
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
                    return new Color(0.85f, 0.85f, 0.85f);
            }
        }

        /// <summary>
        /// Devuelve el color hexadecimal usado por TextMeshPro para colorear partes concretas del HUD.
        /// </summary>
        private string GetRiskHexColor(RiskLevel riskLevel)
        {
            switch (riskLevel)
            {
                case RiskLevel.High:
                    return "#FF3333";

                case RiskLevel.Medium:
                    return "#FFCC33";

                default:
                    return "#E6E6E6";
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
    }
}
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
 * - sector hacia el que mira el piloto,
 * - sector donde se encuentra la aeronave,
 * - predicción inicial de conflicto,
 * - nivel de riesgo,
 * - mensaje superior de alerta,
 * - retícula central.
 *
 * Se conecta con:
 * - ExternalGpsProvider: actualiza el panel izquierdo con GPS REAL / WAIT.
 * - OpenSkyApiClient: envía un TrafficSnapshot con tráfico, target y predicción.
 * - TrafficSnapshot: modelo de datos que contiene la información que se muestra.
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

            RenderSystemStatus("ONLINE", "SIM", "2D", "1 Hz");
            RenderTraffic(new TrafficSnapshot(0, "--", RiskLevel.Low, "NO ALERTS"));
        }

        /// <summary>
        /// Activa Rich Text en todos los TextMeshPro para poder usar tamaños y colores dentro del texto.
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
                $"SYS  {FormatHudValue(status)}\n" +
                $"GPS  {FormatHudValue(gpsStatus)}\n" +
                $"MODE {FormatHudValue(mode)}\n" +
                $"UPD  {FormatHudValue(updateRate)}";
        }

        /// <summary>
        /// Actualiza el panel derecho, la alerta superior y la retícula central.
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
        /// Construye el texto del panel derecho con una estructura más simple e intuitiva.
        /// </summary>
        private string BuildTrafficText(TrafficSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return
                    "<size=78%>AIRSPACE</size>\n" +
                    "NO DATA\n\n" +
                    "TRAF --\n" +
                    "RISK --";
            }

            string riskLabel = GetRiskLabel(snapshot.RiskLevel);
            string riskColor = GetRiskHexColor(snapshot.RiskLevel);

            if (!HasRelevantAircraft(snapshot))
            {
                return BuildNoTargetText(snapshot, riskLabel, riskColor);
            }

            return BuildTargetText(snapshot, riskLabel, riskColor);
        }

        /// <summary>
        /// Construye el panel cuando existe una aeronave seleccionada como objetivo.
        /// </summary>
        private string BuildTargetText(TrafficSnapshot snapshot, string riskLabel, string riskColor)
        {
            string viewSector = ExtractSectorName(snapshot.ViewSector);
            string targetSector = ExtractSectorName(snapshot.TargetSector);
            string selectionMode = FormatSelectionMode(snapshot.SelectionMode);
            string predictionBlock = BuildPredictionBlock(snapshot);

            return
                "<size=78%>AIRSPACE</size>\n" +
                BuildCabinRadar(snapshot) + "\n\n" +
                $"<size=72%>VIEW {viewSector} · {selectionMode}</size>\n" +
                $"<size=125%>{FormatHudValue(snapshot.RelevantCallsign)}</size>\n" +
                $"<size=82%>{targetSector} · {FormatHudValue(snapshot.NearestDistance)}</size>\n\n" +
                predictionBlock +
                $"ALT  {FormatHudValue(snapshot.RelevantAltitude)}\n" +
                $"HDG  {FormatHudValue(snapshot.RelevantHeading)}\n" +
                $"TRAF {snapshot.NearbyAircraft}\n" +
                $"RISK <color={riskColor}>{riskLabel}</color>";
        }

        /// <summary>
        /// Construye el panel cuando no hay aeronave objetivo seleccionada.
        /// </summary>
        private string BuildNoTargetText(TrafficSnapshot snapshot, string riskLabel, string riskColor)
        {
            string viewSector = ExtractSectorName(snapshot.ViewSector);
            string predictionBlock = BuildPredictionBlock(snapshot);

            return
                "<size=78%>AIRSPACE</size>\n" +
                BuildCabinRadar(snapshot) + "\n\n" +
                $"<size=72%>VIEW {viewSector}</size>\n" +
                "<size=115%>NO TARGET</size>\n\n" +
                predictionBlock +
                $"TRAF {snapshot.NearbyAircraft}\n" +
                $"NEAR {FormatHudValue(snapshot.NearestDistance)}\n" +
                $"RISK <color={riskColor}>{riskLabel}</color>";
        }

        /// <summary>
        /// Construye una línea de radar compacta con los cuatro sectores principales de cabina.
        /// El punto coloreado indica el sector donde está el target.
        /// </summary>
        private string BuildCabinRadar(TrafficSnapshot snapshot)
        {
            string targetSector = snapshot != null
                ? ExtractSectorName(snapshot.TargetSector)
                : "--";

            RiskLevel riskLevel = snapshot != null
                ? snapshot.RiskLevel
                : RiskLevel.Low;

            string front = BuildRadarDot("FRONT", targetSector, riskLevel);
            string left = BuildRadarDot("LEFT", targetSector, riskLevel);
            string right = BuildRadarDot("RIGHT", targetSector, riskLevel);
            string rear = BuildRadarDot("REAR", targetSector, riskLevel);

            return $"RADAR F{front}  L{left}  R{right}  B{rear}";
        }

        /// <summary>
        /// Devuelve el punto del radar para un sector concreto.
        /// </summary>
        private string BuildRadarDot(string sectorName, string targetSector, RiskLevel riskLevel)
        {
            if (targetSector == sectorName)
            {
                return $"<color={GetRiskHexColor(riskLevel)}>●</color>";
            }

            return "·";
        }

        /// <summary>
        /// Construye el bloque de predicción del HUD.
        /// Si no hay predicción fiable, muestra espera.
        /// Si el riesgo es bajo, muestra camino despejado sin saturar con CPA/TCPA.
        /// Si hay riesgo medio o alto, muestra CPA e IN.
        /// </summary>
        private string BuildPredictionBlock(TrafficSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "";
            }

            string status = string.IsNullOrWhiteSpace(snapshot.ConflictStatus)
                ? "PRED WAIT"
                : snapshot.ConflictStatus;

            bool hasCpa = !string.IsNullOrWhiteSpace(snapshot.ClosestApproachDistance) &&
                        snapshot.ClosestApproachDistance != "--";

            bool hasTcpa = !string.IsNullOrWhiteSpace(snapshot.TimeToClosestApproach) &&
                        snapshot.TimeToClosestApproach != "--";

            if (!hasCpa || !hasTcpa)
            {
                return $"{status}\n";
            }

            if (snapshot.RiskLevel == RiskLevel.Low)
            {
                return "PATH CLEAR\n";
            }

            return
                $"{status}\n" +
                $"CPA  {snapshot.ClosestApproachDistance}\n" +
                $"IN   {snapshot.TimeToClosestApproach}\n";
        }

        /// <summary>
        /// Devuelve un estado de predicción claro cuando todavía no hay CPA/TCPA fiable.
        /// </summary>
        private string FormatPredictionStatus(string motionStatus, string conflictStatus)
        {
            if (!string.IsNullOrWhiteSpace(motionStatus) &&
                motionStatus.ToUpperInvariant().Contains("WAIT"))
            {
                return "PRED WAIT";
            }

            if (!string.IsNullOrWhiteSpace(conflictStatus) &&
                conflictStatus.ToUpperInvariant().Contains("NO PREDICTION"))
            {
                return "PRED WAIT";
            }

            if (!string.IsNullOrWhiteSpace(motionStatus))
            {
                return motionStatus;
            }

            return "PRED WAIT";
        }

        /// <summary>
        /// Simplifica el estado de conflicto para mostrarlo de forma breve en el HUD.
        /// </summary>
        private string FormatConflictStatus(string conflictStatus)
        {
            if (string.IsNullOrWhiteSpace(conflictStatus))
            {
                return "PRED OK";
            }

            string upper = conflictStatus.ToUpperInvariant();

            if (upper.Contains("CONFLICT"))
            {
                return "CONFLICT";
            }

            if (upper.Contains("WATCH"))
            {
                return "WATCH";
            }

            if (upper.Contains("CLEAR"))
            {
                return "PATH CLEAR";
            }

            return conflictStatus;
        }

        /// <summary>
        /// Comprueba si el snapshot contiene una aeronave destacada.
        /// </summary>
        private bool HasRelevantAircraft(TrafficSnapshot snapshot)
        {
            return snapshot != null &&
                   !string.IsNullOrWhiteSpace(snapshot.RelevantCallsign);
        }

        /// <summary>
        /// Comprueba si un valor textual es realmente mostrable.
        /// </summary>
        private bool HasDisplayValue(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value != "--";
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
        /// Simplifica el modo de selección para mostrarlo compacto en el HUD.
        /// </summary>
        private string FormatSelectionMode(string selectionMode)
        {
            if (string.IsNullOrWhiteSpace(selectionMode))
            {
                return "NO LOCK";
            }

            string upper = selectionMode.ToUpperInvariant();

            if (upper.Contains("CONFLICT"))
            {
                return "CONFLICT";
            }

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
        /// Actualiza la retícula central según si hay target y según el nivel de riesgo.
        /// </summary>
        private void UpdateReticle(TrafficSnapshot snapshot)
        {
            if (reticleText == null)
            {
                return;
            }

            if (snapshot == null || !HasRelevantAircraft(snapshot))
            {
                reticleText.text = "+";
                reticleText.color = new Color(0.85f, 0.85f, 0.85f);
                return;
            }

            switch (snapshot.RiskLevel)
            {
                case RiskLevel.High:
                    reticleText.text = "[!]";
                    break;

                case RiskLevel.Medium:
                    reticleText.text = "[!]";
                    break;

                default:
                    reticleText.text = "[+]";
                    break;
            }

            reticleText.color = GetRiskColor(snapshot.RiskLevel);
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

            string safeMessage = string.IsNullOrWhiteSpace(message)
                ? "NO ALERTS"
                : message;

            switch (riskLevel)
            {
                case RiskLevel.High:
                    alertText.text = $"!!! {safeMessage} !!!";
                    break;

                case RiskLevel.Medium:
                    alertText.text = $"-- {safeMessage} --";
                    break;

                default:
                    alertText.text = safeMessage;
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

        /// <summary>
        /// Devuelve un valor seguro para mostrar en el HUD cuando un dato opcional no está disponible.
        /// </summary>
        private string FormatHudValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "--" : value;
        }
    }
}

/*
 * HudController.cs
 * ------------------------------------------------------------
 * Este script controla los textos principales del HUD del visor.
 *
 * Su función es recibir datos ya procesados por otros módulos y mostrarlos
 * en pantalla de forma clara y adaptativa:
 * - estado del sistema,
 * - estado del GPS,
 * - tráfico aéreo cercano,
 * - aeronave seleccionada como TARGET,
 * - guía visual según la dirección del target,
 * - predicción inicial de conflicto,
 * - nivel de riesgo,
 * - mensaje superior de alerta,
 * - retícula central.
 *
 * La filosofía del HUD es no saturar al piloto:
 * - LOW: información mínima y discreta.
 * - MED: aviso moderado con CPA/TCPA y guía visual.
 * - HIGH: alerta clara con información crítica y guía visual destacada.
 *
 * Se conecta con:
 * - ExternalGpsProvider: actualiza el panel izquierdo con GPS REAL / WAIT.
 * - OpenSkyApiClient: envía un TrafficSnapshot con tráfico, target y predicción.
 * - TrafficSnapshot: modelo de datos que contiene la información que se muestra.
 */

using System;
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

        [Header("HUD Visual References")]
        [SerializeField] private WorldTargetBox worldTargetBox;

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

            if (worldTargetBox != null)
            {
                worldTargetBox.RenderBox(snapshot);
            }

            if (snapshot != null)
            {
                RenderAlert(snapshot.AlertMessage, snapshot.RiskLevel);
            }
        }

        /// <summary>
        /// Construye el texto principal del panel derecho de forma adaptativa según el riesgo.
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

            if (snapshot.RiskLevel == RiskLevel.Low)
            {
                return BuildLowRiskText(snapshot, riskLabel, riskColor);
            }

            if (!HasRelevantAircraft(snapshot))
            {
                return BuildNoTargetWarningText(snapshot, riskLabel, riskColor);
            }

            return BuildActiveRiskTargetText(snapshot, riskLabel, riskColor);
        }

        /// <summary>
        /// Construye un HUD mínimo cuando no hay riesgo relevante.
        /// </summary>
        private string BuildLowRiskText(TrafficSnapshot snapshot, string riskLabel, string riskColor)
        {
            string nearestLine = HasRelevantAircraft(snapshot)
                ? $"NEAREST {snapshot.RelevantCallsign}\n{ExtractSectorName(snapshot.TargetSector)} · {FormatHudValue(snapshot.NearestDistance)}\n"
                : $"NEAREST {FormatHudValue(snapshot.NearestDistance)}\n";

            string scenarioLine = BuildScenarioLine(snapshot.SelectionMode);

            return
                "<size=78%>AIRSPACE</size>\n" +
                "<size=120%>PATH CLEAR</size>\n" +
                scenarioLine +
                "\n" +
                nearestLine +
                $"TRAF {snapshot.NearbyAircraft}\n" +
                $"RISK <color={riskColor}>{riskLabel}</color>";
        }

        /// <summary>
        /// Construye el HUD cuando hay riesgo medio o alto y existe una aeronave relevante.
        /// </summary>
        private string BuildActiveRiskTargetText(TrafficSnapshot snapshot, string riskLabel, string riskColor)
        {
            string viewSector = ExtractSectorName(snapshot.ViewSector);
            string targetSector = ExtractSectorName(snapshot.TargetSector);
            string selectionMode = FormatSelectionMode(snapshot.SelectionMode);
            string status = FormatConflictStatus(snapshot.ConflictStatus, snapshot.RiskLevel);
            string guidance = BuildVisualGuidanceText(snapshot);

            return
                "<size=78%>AIRSPACE</size>\n" +
                $"<size=120%><color={riskColor}>{status}</color></size>\n" +
                BuildCabinSectorLine(snapshot) + "\n" +
                $"<size=70%>VIEW {viewSector} · {selectionMode}</size>\n" +
                $"<size=125%>{snapshot.RelevantCallsign}</size>\n" +
                $"<size=82%>{targetSector} · {FormatHudValue(snapshot.NearestDistance)}</size>\n" +
                $"<size=74%>{guidance}</size>\n\n" +
                BuildPredictionBlock(snapshot) +
                $"ALT  {FormatHudValue(snapshot.RelevantAltitude)}\n" +
                $"HDG  {FormatHudValue(snapshot.RelevantHeading)}\n" +
                $"TRAF {snapshot.NearbyAircraft}\n" +
                $"RISK <color={riskColor}>{riskLabel}</color>";
        }

        /// <summary>
        /// Construye el HUD cuando hay riesgo, pero no existe una aeronave relevante que mostrar.
        /// </summary>
        private string BuildNoTargetWarningText(TrafficSnapshot snapshot, string riskLabel, string riskColor)
        {
            string viewSector = ExtractSectorName(snapshot.ViewSector);
            string status = FormatConflictStatus(snapshot.ConflictStatus, snapshot.RiskLevel);

            return
                "<size=78%>AIRSPACE</size>\n" +
                $"<size=120%><color={riskColor}>{status}</color></size>\n" +
                $"<size=70%>VIEW {viewSector}</size>\n" +
                "<size=115%>NO TARGET</size>\n\n" +
                BuildPredictionBlock(snapshot) +
                $"TRAF {snapshot.NearbyAircraft}\n" +
                $"NEAR {FormatHudValue(snapshot.NearestDistance)}\n" +
                $"RISK <color={riskColor}>{riskLabel}</color>";
        }

        /// <summary>
        /// Construye una línea compacta de sectores de cabina.
        /// No es un radar real: indica en qué sector relativo está el target.
        /// </summary>
        private string BuildCabinSectorLine(TrafficSnapshot snapshot)
        {
            string targetSector = snapshot != null
                ? ExtractSectorName(snapshot.TargetSector)
                : "--";

            RiskLevel riskLevel = snapshot != null
                ? snapshot.RiskLevel
                : RiskLevel.Low;

            string front = BuildSectorDot("FRONT", targetSector, riskLevel);
            string left = BuildSectorDot("LEFT", targetSector, riskLevel);
            string right = BuildSectorDot("RIGHT", targetSector, riskLevel);
            string rear = BuildSectorDot("REAR", targetSector, riskLevel);

            return $"<size=72%>SECTOR F{front}  L{left}  R{right}  B{rear}</size>";
        }

        /// <summary>
        /// Devuelve el punto de sector para indicar dónde está el target.
        /// </summary>
        private string BuildSectorDot(string sectorName, string targetSector, RiskLevel riskLevel)
        {
            if (targetSector == sectorName)
            {
                return $"<color={GetRiskHexColor(riskLevel)}>●</color>";
            }

            return "·";
        }

        /// <summary>
        /// Construye el bloque de predicción del HUD.
        /// En LOW se oculta CPA/TCPA para no saturar.
        /// En MED/HIGH se muestra CPA/TCPA porque es información crítica.
        /// </summary>
        private string BuildPredictionBlock(TrafficSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "";
            }

            bool hasCpa = HasDisplayValue(snapshot.ClosestApproachDistance);
            bool hasTcpa = HasDisplayValue(snapshot.TimeToClosestApproach);

            if (!hasCpa || !hasTcpa)
            {
                return "PRED WAIT\n";
            }

            if (snapshot.RiskLevel == RiskLevel.Low)
            {
                return "";
            }

            return
                $"CPA  {snapshot.ClosestApproachDistance}\n" +
                $"IN   {snapshot.TimeToClosestApproach}\n";
        }

        /// <summary>
        /// Construye una línea opcional para indicar que se está usando un escenario de prueba.
        /// </summary>
        private string BuildScenarioLine(string selectionMode)
        {
            if (string.IsNullOrWhiteSpace(selectionMode))
            {
                return "";
            }

            string upper = selectionMode.ToUpperInvariant();

            if (!upper.Contains("SCENARIO"))
            {
                return "";
            }

            return $"<size=64%>{selectionMode}</size>\n";
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

            if (upper.Contains("SCENARIO"))
            {
                return "TEST MODE";
            }

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
        /// Simplifica el estado de conflicto para mostrarlo como encabezado principal.
        /// </summary>
        private string FormatConflictStatus(string conflictStatus, RiskLevel riskLevel)
        {
            if (riskLevel == RiskLevel.High)
            {
                return "CONFLICT RISK";
            }

            if (riskLevel == RiskLevel.Medium)
            {
                return "TRAJECTORY WATCH";
            }

            if (string.IsNullOrWhiteSpace(conflictStatus))
            {
                return "PATH CLEAR";
            }

            string upper = conflictStatus.ToUpperInvariant();

            if (upper.Contains("CONFLICT"))
            {
                return "CONFLICT RISK";
            }

            if (upper.Contains("WATCH"))
            {
                return "TRAJECTORY WATCH";
            }

            return "PATH CLEAR";
        }

        /// <summary>
        /// Actualiza la retícula central según el nivel de riesgo y la dirección angular del target.
        /// </summary>
        private void UpdateReticle(TrafficSnapshot snapshot)
        {
            if (reticleText == null)
            {
                return;
            }

            if (snapshot == null || !HasRelevantAircraft(snapshot) || snapshot.RiskLevel == RiskLevel.Low)
            {
                reticleText.text = "+";
                reticleText.color = new Color(0.85f, 0.85f, 0.85f);
                return;
            }

            reticleText.text = BuildReticleMarker(snapshot);
            reticleText.color = GetRiskColor(snapshot.RiskLevel);
        }

        /// <summary>
        /// Construye el marcador central del HUD según la posición angular del target respecto a la mirada.
        /// </summary>
        private string BuildReticleMarker(TrafficSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.TargetViewOffsetDegrees.HasValue)
            {
                return snapshot != null && snapshot.RiskLevel == RiskLevel.High ? "[!]" : "[+]";
            }

            double offset = snapshot.TargetViewOffsetDegrees.Value;
            double absoluteOffset = Math.Abs(offset);

            if (absoluteOffset <= 12.0)
            {
                return snapshot.RiskLevel == RiskLevel.High ? "[!]" : "[+]";
            }

            if (absoluteOffset <= 45.0)
            {
                return offset > 0.0 ? "+ >" : "< +";
            }

            if (absoluteOffset <= 120.0)
            {
                return offset > 0.0 ? ">>" : "<<";
            }

            return "v";
        }

        /// <summary>
        /// Construye una indicación textual breve para orientar al piloto hacia el target.
        /// Solo se muestra cuando el riesgo es MED/HIGH.
        /// </summary>
        private string BuildVisualGuidanceText(TrafficSnapshot snapshot)
        {
            if (snapshot == null || !HasRelevantAircraft(snapshot))
            {
                return "";
            }

            if (snapshot.RiskLevel == RiskLevel.Low)
            {
                return "";
            }

            if (!snapshot.TargetViewOffsetDegrees.HasValue)
            {
                return "TARGET DIRECTION --";
            }

            double offset = snapshot.TargetViewOffsetDegrees.Value;
            double absoluteOffset = Math.Abs(offset);

            if (absoluteOffset <= 12.0)
            {
                return "TARGET CENTERED";
            }

            if (absoluteOffset <= 45.0)
            {
                return offset > 0.0
                    ? "SLIGHT RIGHT >"
                    : "< SLIGHT LEFT";
            }

            if (absoluteOffset <= 120.0)
            {
                return offset > 0.0
                    ? "LOOK RIGHT >>"
                    : "<< LOOK LEFT";
            }

            return "TARGET BEHIND";
        }

        /// <summary>
        /// Actualiza el mensaje superior de alerta y lo hace más visible según el nivel de riesgo.
        /// En LOW se deja vacío para no invadir el campo de visión.
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
                    alertText.text = "";
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

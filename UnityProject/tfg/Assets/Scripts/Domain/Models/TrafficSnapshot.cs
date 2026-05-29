/*
 * TrafficSnapshot.cs
 * ------------------------------------------------------------
 * Este modelo representa la información resumida que se muestra en el HUD
 * sobre el tráfico aéreo cercano.
 *
 * Guarda el resumen general del tráfico, el riesgo calculado, los datos de
 * la aeronave seleccionada como TARGET, la información de predicción de
 * conflicto y una indicación angular visual del target respecto a la mirada.
 *
 * Se conecta con:
 * - OpenSkyApiClient: crea este snapshot con datos reales o simulados.
 * - HudController: lee este snapshot y lo muestra en el panel derecho del visor.
 */

namespace TFG.ARVisor.Domain.Models
{
    public class TrafficSnapshot
    {
        public int NearbyAircraft { get; }
        public string NearestDistance { get; }
        public RiskLevel RiskLevel { get; }
        public string AlertMessage { get; }

        public string RelevantCallsign { get; }
        public string RelevantCountry { get; }
        public string RelevantAltitude { get; }
        public string RelevantHeading { get; }

        public string ViewSector { get; }
        public string TargetSector { get; }
        public string SelectionMode { get; }

        public string ConflictStatus { get; }
        public string ClosestApproachDistance { get; }
        public string TimeToClosestApproach { get; }
        public string MotionStatus { get; }

        public double? TargetViewOffsetDegrees { get; }

        /// <summary>
        /// Crea el resumen de tráfico que será mostrado en el HUD.
        /// </summary>
        public TrafficSnapshot(
            int nearbyAircraft,
            string nearestDistance,
            RiskLevel riskLevel,
            string alertMessage,
            string relevantCallsign = "",
            string relevantCountry = "",
            string relevantAltitude = "",
            string relevantHeading = "",
            string viewSector = "",
            string targetSector = "",
            string selectionMode = "",
            string conflictStatus = "",
            string closestApproachDistance = "",
            string timeToClosestApproach = "",
            string motionStatus = "",
            double? targetViewOffsetDegrees = null)
        {
            NearbyAircraft = nearbyAircraft;
            NearestDistance = nearestDistance;
            RiskLevel = riskLevel;
            AlertMessage = alertMessage;

            RelevantCallsign = relevantCallsign;
            RelevantCountry = relevantCountry;
            RelevantAltitude = relevantAltitude;
            RelevantHeading = relevantHeading;

            ViewSector = viewSector;
            TargetSector = targetSector;
            SelectionMode = selectionMode;

            ConflictStatus = conflictStatus;
            ClosestApproachDistance = closestApproachDistance;
            TimeToClosestApproach = timeToClosestApproach;
            MotionStatus = motionStatus;

            TargetViewOffsetDegrees = targetViewOffsetDegrees;
        }
    }
}
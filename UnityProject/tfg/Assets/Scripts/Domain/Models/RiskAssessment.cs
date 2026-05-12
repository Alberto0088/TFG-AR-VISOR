/*
 * RiskAssessment.cs
 * ------------------------------------------------------------
 * Este modelo representa el resultado de analizar el riesgo asociado
 * al tráfico aéreo cercano.
 *
 * Su función es agrupar en un solo objeto:
 * - el nivel de riesgo calculado,
 * - el mensaje de alerta asociado,
 * - la aeronave más relevante,
 * - la distancia de la aeronave más cercana,
 * - la diferencia de altitud si está disponible.
 *
 * Se conecta con:
 * - RiskEngine: genera este resultado a partir de las aeronaves cercanas.
 * - OpenSkyApiClient: usará este resultado para actualizar el HUD.
 * - HudController: mostrará el nivel de riesgo y el mensaje de alerta.
 */

namespace TFG.ARVisor.Domain.Models
{
    public class RiskAssessment
    {
        public RiskLevel RiskLevel { get; }
        public string AlertMessage { get; }
        public AircraftGeoState MostRelevantAircraft { get; }
        public double? NearestDistanceKm { get; }
        public double? AltitudeDifferenceMeters { get; }
        public int AircraftCount { get; }

        /// <summary>
        /// Crea el resultado del análisis de riesgo calculado para el tráfico cercano.
        /// </summary>
        public RiskAssessment(
            RiskLevel riskLevel,
            string alertMessage,
            AircraftGeoState mostRelevantAircraft,
            double? nearestDistanceKm,
            double? altitudeDifferenceMeters,
            int aircraftCount)
        {
            RiskLevel = riskLevel;
            AlertMessage = alertMessage;
            MostRelevantAircraft = mostRelevantAircraft;
            NearestDistanceKm = nearestDistanceKm;
            AltitudeDifferenceMeters = altitudeDifferenceMeters;
            AircraftCount = aircraftCount;
        }
    }
}
/*
 * TrafficSnapshot.cs
 * ------------------------------------------------------------
 * Este modelo representa la información resumida que se muestra en el HUD
 * sobre el tráfico aéreo cercano.
 *
 * Guarda tanto el resumen general:
 * - número de aeronaves cercanas,
 * - distancia de la aeronave más próxima,
 * - nivel de riesgo,
 * - mensaje de alerta,
 *
 * como información opcional de la aeronave más relevante:
 * - callsign,
 * - país,
 * - altitud,
 * - rumbo.
 *
 * Se conecta con:
 * - OpenSkyApiClient: crea este snapshot con datos reales de OpenSky.
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

        /// <summary>
        /// Crea el resumen de tráfico que será mostrado en el HUD.
        /// Los datos de aeronave relevante son opcionales para mantener compatibilidad con pruebas antiguas.
        /// </summary>
        public TrafficSnapshot(
            int nearbyAircraft,
            string nearestDistance,
            RiskLevel riskLevel,
            string alertMessage,
            string relevantCallsign = "",
            string relevantCountry = "",
            string relevantAltitude = "",
            string relevantHeading = "")
        {
            NearbyAircraft = nearbyAircraft;
            NearestDistance = nearestDistance;
            RiskLevel = riskLevel;
            AlertMessage = alertMessage;
            RelevantCallsign = relevantCallsign;
            RelevantCountry = relevantCountry;
            RelevantAltitude = relevantAltitude;
            RelevantHeading = relevantHeading;
        }
    }
}
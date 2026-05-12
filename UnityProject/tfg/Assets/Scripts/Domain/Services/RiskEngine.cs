/*
 * RiskEngine.cs
 * ------------------------------------------------------------
 * Este servicio calcula un primer nivel de riesgo a partir de las
 * aeronaves cercanas recibidas desde OpenSky.
 *
 * Funcionamiento general:
 * 1. Recibe la posición propia del usuario/avión.
 * 2. Recibe la lista de aeronaves ya filtradas por distancia.
 * 3. Busca la aeronave más cercana.
 * 4. Calcula la distancia horizontal.
 * 5. Si hay altitud fiable, también calcula diferencia vertical.
 * 6. Devuelve un RiskAssessment con nivel LOW, MEDIUM o HIGH.
 *
 * Este cálculo es una primera versión para el prototipo. Más adelante
 * se podrá mejorar teniendo en cuenta rumbo, velocidad relativa y
 * orientación del visor.
 *
 * Se conecta con:
 * - OwnshipGeoState: posición propia.
 * - AircraftGeoState: aeronaves cercanas.
 * - GeoDistanceCalculator: cálculo de distancia horizontal.
 * - OpenSkyApiClient: usará este motor para actualizar el HUD.
 */

using System;
using System.Collections.Generic;
using TFG.ARVisor.Domain.Models;

namespace TFG.ARVisor.Domain.Services
{
    public static class RiskEngine
    {
        private const double HighRiskDistance2D = 10.0;
        private const double MediumRiskDistance2D = 30.0;

        private const double HighRiskDistance3D = 10.0;
        private const double MediumRiskDistance3D = 30.0;

        private const double HighRiskAltitudeDifferenceMeters = 300.0;
        private const double MediumRiskAltitudeDifferenceMeters = 700.0;

        /// <summary>
        /// Evalúa el riesgo del tráfico cercano respecto a la posición propia.
        /// </summary>
        public static RiskAssessment Evaluate(
            OwnshipGeoState ownshipState,
            List<AircraftGeoState> aircraft)
        {
            if (ownshipState == null)
            {
                throw new ArgumentNullException(nameof(ownshipState));
            }

            if (aircraft == null || aircraft.Count == 0)
            {
                return new RiskAssessment(
                    RiskLevel.Low,
                    "NO ALERTS",
                    null,
                    null,
                    null,
                    0
                );
            }

            AircraftGeoState nearestAircraft = null;
            double nearestDistanceKm = double.MaxValue;
            double? nearestAltitudeDifference = null;

            foreach (AircraftGeoState item in aircraft)
            {
                double distanceKm = GeoDistanceCalculator.DistanceKm(ownshipState, item);

                if (distanceKm < nearestDistanceKm)
                {
                    nearestDistanceKm = distanceKm;
                    nearestAircraft = item;
                    nearestAltitudeDifference = CalculateAltitudeDifference(ownshipState, item);
                }
            }

            RiskLevel riskLevel = CalculateRiskLevel(
                ownshipState,
                nearestDistanceKm,
                nearestAltitudeDifference
            );

            string alertMessage = GetAlertMessage(riskLevel);

            return new RiskAssessment(
                riskLevel,
                alertMessage,
                nearestAircraft,
                nearestDistanceKm,
                nearestAltitudeDifference,
                aircraft.Count
            );
        }

        /// <summary>
        /// Calcula la diferencia absoluta de altitud entre la posición propia y una aeronave.
        /// </summary>
        private static double? CalculateAltitudeDifference(
            OwnshipGeoState ownshipState,
            AircraftGeoState aircraft)
        {
            if (!ownshipState.AltitudeMeters.HasValue || !aircraft.AltitudeMeters.HasValue)
            {
                return null;
            }

            return Math.Abs(ownshipState.AltitudeMeters.Value - aircraft.AltitudeMeters.Value);
        }

        /// <summary>
        /// Calcula el nivel de riesgo usando distancia horizontal y, si es fiable, diferencia de altitud.
        /// </summary>
        private static RiskLevel CalculateRiskLevel(
            OwnshipGeoState ownshipState,
            double nearestDistanceKm,
            double? altitudeDifferenceMeters)
        {
            if (ownshipState.Mode == OwnshipMode.Mode3D && altitudeDifferenceMeters.HasValue)
            {
                return Calculate3DRisk(nearestDistanceKm, altitudeDifferenceMeters.Value);
            }

            return Calculate2DRisk(nearestDistanceKm);
        }

        /// <summary>
        /// Calcula riesgo en modo 2D cuando no hay altitud fiable.
        /// </summary>
        private static RiskLevel Calculate2DRisk(double distanceKm)
        {
            if (distanceKm <= HighRiskDistance2D)
            {
                return RiskLevel.High;
            }

            if (distanceKm <= MediumRiskDistance2D)
            {
                return RiskLevel.Medium;
            }

            return RiskLevel.Low;
        }

        /// <summary>
        /// Calcula riesgo en modo 3D usando distancia horizontal y separación vertical.
        /// </summary>
        private static RiskLevel Calculate3DRisk(
            double distanceKm,
            double altitudeDifferenceMeters)
        {
            if (distanceKm <= HighRiskDistance3D &&
                altitudeDifferenceMeters <= HighRiskAltitudeDifferenceMeters)
            {
                return RiskLevel.High;
            }

            if (distanceKm <= MediumRiskDistance3D &&
                altitudeDifferenceMeters <= MediumRiskAltitudeDifferenceMeters)
            {
                return RiskLevel.Medium;
            }

            return RiskLevel.Low;
        }

        /// <summary>
        /// Devuelve el mensaje de alerta asociado al nivel de riesgo calculado.
        /// </summary>
        private static string GetAlertMessage(RiskLevel riskLevel)
        {
            switch (riskLevel)
            {
                case RiskLevel.High:
                    return "COLLISION RISK";

                case RiskLevel.Medium:
                    return "TRAFFIC ADVISORY";

                default:
                    return "NO ALERTS";
            }
        }
    }
}
/*
 * ConflictPredictionEngine.cs
 * ------------------------------------------------------------
 * Este servicio predice conflictos iniciales de trayectoria entre la posición
 * propia y las aeronaves cercanas.
 *
 * Utiliza una aproximación CPA/TCPA:
 * - CPA: distancia mínima prevista entre dos trayectorias.
 * - TCPA: tiempo hasta esa distancia mínima.
 *
 * Funcionamiento general:
 * 1. Convierte la posición relativa de la aeronave a metros Este/Norte.
 * 2. Convierte rumbo y velocidad propia a vector de velocidad.
 * 3. Convierte rumbo y velocidad de la aeronave a vector de velocidad.
 * 4. Calcula movimiento relativo.
 * 5. Estima el punto de máxima aproximación en una ventana temporal.
 * 6. Clasifica el riesgo según distancia prevista y tiempo.
 *
 * Es una primera versión académica para prototipo. No representa un sistema
 * certificado de prevención de colisiones.
 *
 * Se conecta con:
 * - OwnshipGeoState: posición propia actual.
 * - OwnshipMotionState: rumbo y velocidad propios estimados.
 * - AircraftGeoState: datos de aeronaves de OpenSky.
 * - ConflictAssessment: resultado de predicción.
 */

using System;
using System.Collections.Generic;
using TFG.ARVisor.Domain.Models;

namespace TFG.ARVisor.Domain.Services
{
    public static class ConflictPredictionEngine
    {
        private const double EarthRadiusMeters = 6371000.0;

        private const double LookAheadSeconds = 900.0;

        private const double HighRiskCpaKm = 5.0;
        private const double MediumRiskCpaKm = 10.0;

        private const double HighRiskTimeSeconds = 600.0;
        private const double MediumRiskTimeSeconds = 900.0;

        /// <summary>
        /// Evalúa todas las aeronaves cercanas y devuelve la predicción más crítica.
        /// </summary>
        public static ConflictAssessment EvaluateMostCritical(
            OwnshipGeoState ownshipState,
            OwnshipMotionState ownshipMotion,
            List<AircraftGeoState> aircraft)
        {
            if (ownshipState == null)
            {
                throw new ArgumentNullException(nameof(ownshipState));
            }

            if (aircraft == null || aircraft.Count == 0)
            {
                return null;
            }

            ConflictAssessment mostCritical = null;

            foreach (AircraftGeoState item in aircraft)
            {
                ConflictAssessment assessment = EvaluateAircraft(
                    ownshipState,
                    ownshipMotion,
                    item
                );

                if (mostCritical == null ||
                    GetRiskPriority(assessment.RiskLevel) > GetRiskPriority(mostCritical.RiskLevel) ||
                    IsSameRiskButMoreUrgent(assessment, mostCritical))
                {
                    mostCritical = assessment;
                }
            }

            return mostCritical;
        }

        /// <summary>
        /// Evalúa la predicción de conflicto contra una aeronave concreta.
        /// </summary>
        public static ConflictAssessment EvaluateAircraft(
            OwnshipGeoState ownshipState,
            OwnshipMotionState ownshipMotion,
            AircraftGeoState aircraft)
        {
            if (ownshipState == null)
            {
                throw new ArgumentNullException(nameof(ownshipState));
            }

            if (aircraft == null)
            {
                throw new ArgumentNullException(nameof(aircraft));
            }

            double currentDistanceKm = GeoDistanceCalculator.DistanceKm(
                ownshipState,
                aircraft
            );

            if (ownshipMotion == null || !ownshipMotion.HasReliableMotion)
            {
                return ConflictAssessment.NoPrediction(
                    aircraft,
                    currentDistanceKm,
                    ownshipMotion != null ? ownshipMotion.Reason : "Ownship motion unavailable."
                );
            }

            if (!aircraft.VelocityMps.HasValue || !aircraft.HeadingDegrees.HasValue)
            {
                return ConflictAssessment.NoPrediction(
                    aircraft,
                    currentDistanceKm,
                    "Aircraft speed or heading unavailable."
                );
            }

            Vector2Meters relativePosition = ToLocalEastNorthMeters(
                ownshipState.Latitude,
                ownshipState.Longitude,
                aircraft.Latitude,
                aircraft.Longitude
            );

            Vector2Meters ownshipVelocity = VelocityFromTrack(
                ownshipMotion.TrackDegrees.Value,
                ownshipMotion.SpeedMps.Value
            );

            Vector2Meters aircraftVelocity = VelocityFromTrack(
                aircraft.HeadingDegrees.Value,
                aircraft.VelocityMps.Value
            );

            Vector2Meters relativeVelocity = new Vector2Meters(
                aircraftVelocity.EastMeters - ownshipVelocity.EastMeters,
                aircraftVelocity.NorthMeters - ownshipVelocity.NorthMeters
            );

            double relativeSpeedSquared = relativeVelocity.SqrMagnitude;

            if (relativeSpeedSquared <= 0.0001)
            {
                return ConflictAssessment.NoPrediction(
                    aircraft,
                    currentDistanceKm,
                    "Relative speed too low."
                );
            }

            double tcpaSeconds = -Dot(relativePosition, relativeVelocity) / relativeSpeedSquared;
            tcpaSeconds = Math.Max(0.0, Math.Min(tcpaSeconds, LookAheadSeconds));

            Vector2Meters closestPosition = new Vector2Meters(
                relativePosition.EastMeters + relativeVelocity.EastMeters * tcpaSeconds,
                relativePosition.NorthMeters + relativeVelocity.NorthMeters * tcpaSeconds
            );

            double cpaDistanceKm = closestPosition.Magnitude / 1000.0;
            double? verticalSeparationMeters = CalculateVerticalSeparation(ownshipState, aircraft);

            RiskLevel riskLevel = ClassifyRisk(
                cpaDistanceKm,
                tcpaSeconds,
                verticalSeparationMeters
            );

            return new ConflictAssessment(
                aircraft,
                true,
                currentDistanceKm,
                cpaDistanceKm,
                tcpaSeconds,
                verticalSeparationMeters,
                riskLevel,
                GetAlertMessage(riskLevel),
                "CPA/TCPA prediction."
            );
        }

        /// <summary>
        /// Convierte diferencia lat/lon a coordenadas locales Este/Norte en metros.
        /// </summary>
        private static Vector2Meters ToLocalEastNorthMeters(
            double originLatitude,
            double originLongitude,
            double targetLatitude,
            double targetLongitude)
        {
            double originLatRad = DegreesToRadians(originLatitude);
            double deltaLatRad = DegreesToRadians(targetLatitude - originLatitude);
            double deltaLonRad = DegreesToRadians(targetLongitude - originLongitude);

            double northMeters = deltaLatRad * EarthRadiusMeters;
            double eastMeters = deltaLonRad * EarthRadiusMeters * Math.Cos(originLatRad);

            return new Vector2Meters(eastMeters, northMeters);
        }

        /// <summary>
        /// Convierte rumbo y velocidad a vector de velocidad Este/Norte.
        /// </summary>
        private static Vector2Meters VelocityFromTrack(double trackDegrees, double speedMps)
        {
            double radians = DegreesToRadians(trackDegrees);

            double east = Math.Sin(radians) * speedMps;
            double north = Math.Cos(radians) * speedMps;

            return new Vector2Meters(east, north);
        }

        /// <summary>
        /// Calcula la separación vertical si ambas altitudes están disponibles.
        /// </summary>
        private static double? CalculateVerticalSeparation(
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
        /// Clasifica el riesgo según CPA, TCPA y separación vertical si está disponible.
        /// </summary>
        private static RiskLevel ClassifyRisk(
            double cpaDistanceKm,
            double tcpaSeconds,
            double? verticalSeparationMeters)
        {
            bool verticalConflictPossible =
                !verticalSeparationMeters.HasValue ||
                verticalSeparationMeters.Value <= 1000.0;

            if (!verticalConflictPossible)
            {
                return RiskLevel.Low;
            }

            if (cpaDistanceKm <= HighRiskCpaKm &&
                tcpaSeconds <= HighRiskTimeSeconds)
            {
                return RiskLevel.High;
            }

            if (cpaDistanceKm <= MediumRiskCpaKm &&
                tcpaSeconds <= MediumRiskTimeSeconds)
            {
                return RiskLevel.Medium;
            }

            return RiskLevel.Low;
        }

        /// <summary>
        /// Devuelve el mensaje de alerta asociado al nivel de riesgo.
        /// </summary>
        private static string GetAlertMessage(RiskLevel riskLevel)
        {
            switch (riskLevel)
            {
                case RiskLevel.High:
                    return "CONFLICT RISK";

                case RiskLevel.Medium:
                    return "TRAFFIC ADVISORY";

                default:
                    return "NO ALERTS";
            }
        }

        /// <summary>
        /// Devuelve una prioridad numérica para comparar niveles de riesgo.
        /// </summary>
        private static int GetRiskPriority(RiskLevel riskLevel)
        {
            switch (riskLevel)
            {
                case RiskLevel.High:
                    return 3;

                case RiskLevel.Medium:
                    return 2;

                default:
                    return 1;
            }
        }

        /// <summary>
        /// En caso de mismo riesgo, prioriza el conflicto con menor TCPA o menor CPA.
        /// </summary>
        private static bool IsSameRiskButMoreUrgent(
            ConflictAssessment candidate,
            ConflictAssessment current)
        {
            if (candidate.RiskLevel != current.RiskLevel)
            {
                return false;
            }

            double candidateTcpa = candidate.TimeToClosestApproachSeconds ?? double.MaxValue;
            double currentTcpa = current.TimeToClosestApproachSeconds ?? double.MaxValue;

            if (candidateTcpa < currentTcpa)
            {
                return true;
            }

            double candidateCpa = candidate.ClosestApproachDistanceKm ?? double.MaxValue;
            double currentCpa = current.ClosestApproachDistanceKm ?? double.MaxValue;

            return candidateCpa < currentCpa;
        }

        /// <summary>
        /// Calcula producto escalar entre dos vectores.
        /// </summary>
        private static double Dot(Vector2Meters first, Vector2Meters second)
        {
            return
                first.EastMeters * second.EastMeters +
                first.NorthMeters * second.NorthMeters;
        }

        /// <summary>
        /// Convierte grados a radianes.
        /// </summary>
        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        private struct Vector2Meters
        {
            public double EastMeters { get; }
            public double NorthMeters { get; }

            public double Magnitude => Math.Sqrt(SqrMagnitude);
            public double SqrMagnitude => EastMeters * EastMeters + NorthMeters * NorthMeters;

            public Vector2Meters(double eastMeters, double northMeters)
            {
                EastMeters = eastMeters;
                NorthMeters = northMeters;
            }
        }
    }
}
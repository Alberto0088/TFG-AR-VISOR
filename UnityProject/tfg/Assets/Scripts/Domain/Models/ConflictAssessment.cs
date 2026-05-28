/*
 * ConflictAssessment.cs
 * ------------------------------------------------------------
 * Este modelo representa el resultado de predecir un posible conflicto entre
 * la posición propia y una aeronave cercana.
 *
 * Guarda datos como:
 * - aeronave evaluada,
 * - distancia actual,
 * - distancia mínima prevista,
 * - tiempo hasta la máxima aproximación,
 * - nivel de riesgo,
 * - motivo de la decisión.
 *
 * Se conecta con:
 * - ConflictPredictionEngine: genera estos resultados.
 * - OpenSkyApiClient: usará el resultado más crítico para actualizar el HUD.
 */

namespace TFG.ARVisor.Domain.Models
{
    public class ConflictAssessment
    {
        public AircraftGeoState Aircraft { get; }
        public bool HasPrediction { get; }
        public double CurrentDistanceKm { get; }
        public double? ClosestApproachDistanceKm { get; }
        public double? TimeToClosestApproachSeconds { get; }
        public double? VerticalSeparationMeters { get; }
        public RiskLevel RiskLevel { get; }
        public string AlertMessage { get; }
        public string Reason { get; }

        /// <summary>
        /// Crea el resultado de predicción de conflicto para una aeronave.
        /// </summary>
        public ConflictAssessment(
            AircraftGeoState aircraft,
            bool hasPrediction,
            double currentDistanceKm,
            double? closestApproachDistanceKm,
            double? timeToClosestApproachSeconds,
            double? verticalSeparationMeters,
            RiskLevel riskLevel,
            string alertMessage,
            string reason)
        {
            Aircraft = aircraft;
            HasPrediction = hasPrediction;
            CurrentDistanceKm = currentDistanceKm;
            ClosestApproachDistanceKm = closestApproachDistanceKm;
            TimeToClosestApproachSeconds = timeToClosestApproachSeconds;
            VerticalSeparationMeters = verticalSeparationMeters;
            RiskLevel = riskLevel;
            AlertMessage = alertMessage;
            Reason = reason;
        }

        /// <summary>
        /// Crea un resultado vacío cuando no se puede predecir el conflicto.
        /// </summary>
        public static ConflictAssessment NoPrediction(
            AircraftGeoState aircraft,
            double currentDistanceKm,
            string reason)
        {
            return new ConflictAssessment(
                aircraft,
                false,
                currentDistanceKm,
                null,
                null,
                null,
                RiskLevel.Low,
                "NO ALERTS",
                reason
            );
        }
    }
}
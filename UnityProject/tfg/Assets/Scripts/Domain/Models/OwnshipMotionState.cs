/*
 * OwnshipMotionState.cs
 * ------------------------------------------------------------
 * Este modelo representa el movimiento estimado de la posición propia.
 *
 * A diferencia de OwnshipGeoState, que guarda la posición actual, este modelo
 * guarda información derivada del historial GPS:
 * - rumbo estimado,
 * - velocidad estimada,
 * - si el movimiento es fiable o no.
 *
 * Se conecta con:
 * - OwnshipMotionEstimator: genera este estado.
 * - ConflictPredictionEngine: usa este estado para predecir conflictos.
 */

namespace TFG.ARVisor.Domain.Models
{
    public class OwnshipMotionState
    {
        public bool HasReliableMotion { get; }
        public double? TrackDegrees { get; }
        public double? SpeedMps { get; }
        public double? AltitudeMeters { get; }
        public string Reason { get; }

        /// <summary>
        /// Crea el estado estimado de movimiento propio.
        /// </summary>
        public OwnshipMotionState(
            bool hasReliableMotion,
            double? trackDegrees,
            double? speedMps,
            double? altitudeMeters,
            string reason)
        {
            HasReliableMotion = hasReliableMotion;
            TrackDegrees = trackDegrees;
            SpeedMps = speedMps;
            AltitudeMeters = altitudeMeters;
            Reason = reason;
        }

        /// <summary>
        /// Crea un estado sin movimiento fiable.
        /// </summary>
        public static OwnshipMotionState NotReliable(string reason, double? altitudeMeters)
        {
            return new OwnshipMotionState(
                false,
                null,
                null,
                altitudeMeters,
                reason
            );
        }
    }
}
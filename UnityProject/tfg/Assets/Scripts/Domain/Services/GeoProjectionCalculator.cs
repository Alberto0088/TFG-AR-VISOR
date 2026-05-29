/*
 * GeoProjectionCalculator.cs
 * ------------------------------------------------------------
 * Servicio para proyectar posiciones geográficas a partir de desplazamientos
 * locales en metros.
 *
 * Permite generar aeronaves simuladas alrededor de la posición propia usando
 * una referencia de rumbo:
 * - forwardMeters: metros por delante del avión propio.
 * - rightMeters: metros hacia la derecha del avión propio.
 *
 * Se conecta con:
 * - ConflictTestScenarioFactory: coloca aeronaves simuladas en escenarios
 *   controlados de conflicto.
 */

using System;

namespace TFG.ARVisor.Domain.Services
{
    public static class GeoProjectionCalculator
    {
        private const double EarthRadiusMeters = 6371000.0;

        /// <summary>
        /// Proyecta una coordenada geográfica a partir de un desplazamiento local relativo
        /// al rumbo de referencia.
        /// </summary>
        public static void ProjectFromLocalOffset(
            double originLatitude,
            double originLongitude,
            double referenceTrackDegrees,
            double forwardMeters,
            double rightMeters,
            out double targetLatitude,
            out double targetLongitude)
        {
            double trackRad = DegreesToRadians(referenceTrackDegrees);
            double rightRad = DegreesToRadians(referenceTrackDegrees + 90.0);

            double eastMeters =
                Math.Sin(trackRad) * forwardMeters +
                Math.Sin(rightRad) * rightMeters;

            double northMeters =
                Math.Cos(trackRad) * forwardMeters +
                Math.Cos(rightRad) * rightMeters;

            double originLatRad = DegreesToRadians(originLatitude);

            double deltaLatRad = northMeters / EarthRadiusMeters;
            double deltaLonRad = eastMeters / (EarthRadiusMeters * Math.Cos(originLatRad));

            targetLatitude = originLatitude + RadiansToDegrees(deltaLatRad);
            targetLongitude = originLongitude + RadiansToDegrees(deltaLonRad);
        }

        /// <summary>
        /// Convierte grados a radianes.
        /// </summary>
        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        /// <summary>
        /// Convierte radianes a grados.
        /// </summary>
        private static double RadiansToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }
    }
}
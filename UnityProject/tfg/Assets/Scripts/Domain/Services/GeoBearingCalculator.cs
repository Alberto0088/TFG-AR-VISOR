/*
 * GeoBearingCalculator.cs
 * ------------------------------------------------------------
 * Este servicio calcula el bearing geográfico entre dos coordenadas.
 *
 * El bearing indica en qué dirección se encuentra una aeronave respecto a
 * la posición propia:
 * - 0 grados: norte
 * - 90 grados: este
 * - 180 grados: sur
 * - 270 grados: oeste
 *
 * También proporciona funciones para normalizar ángulos y calcular
 * diferencias angulares.
 *
 * Se conecta con:
 * - OwnshipGeoState: posición propia.
 * - AircraftGeoState: posición de la aeronave.
 * - AircraftTargetSelector: usa el bearing para decidir qué aeronave mostrar.
 */

using System;
using TFG.ARVisor.Domain.Models;

namespace TFG.ARVisor.Domain.Services
{
    public static class GeoBearingCalculator
    {
        /// <summary>
        /// Calcula el bearing geográfico desde la posición propia hasta una aeronave.
        /// </summary>
        public static double BearingDegrees(OwnshipGeoState ownshipState, AircraftGeoState aircraft)
        {
            if (ownshipState == null)
            {
                throw new ArgumentNullException(nameof(ownshipState));
            }

            if (aircraft == null)
            {
                throw new ArgumentNullException(nameof(aircraft));
            }

            return BearingDegrees(
                ownshipState.Latitude,
                ownshipState.Longitude,
                aircraft.Latitude,
                aircraft.Longitude
            );
        }

        /// <summary>
        /// Calcula el bearing geográfico entre dos coordenadas.
        /// </summary>
        public static double BearingDegrees(
            double originLatitude,
            double originLongitude,
            double targetLatitude,
            double targetLongitude)
        {
            double originLatRad = DegreesToRadians(originLatitude);
            double targetLatRad = DegreesToRadians(targetLatitude);
            double deltaLonRad = DegreesToRadians(targetLongitude - originLongitude);

            double y = Math.Sin(deltaLonRad) * Math.Cos(targetLatRad);
            double x =
                Math.Cos(originLatRad) * Math.Sin(targetLatRad) -
                Math.Sin(originLatRad) * Math.Cos(targetLatRad) * Math.Cos(deltaLonRad);

            double bearingRad = Math.Atan2(y, x);
            double bearingDeg = RadiansToDegrees(bearingRad);

            return Normalize360(bearingDeg);
        }

        /// <summary>
        /// Normaliza un ángulo al rango 0-360 grados.
        /// </summary>
        public static double Normalize360(double degrees)
        {
            double normalized = degrees % 360.0;

            if (normalized < 0)
            {
                normalized += 360.0;
            }

            return normalized;
        }

        /// <summary>
        /// Normaliza un ángulo al rango -180 a 180 grados.
        /// </summary>
        public static double NormalizeSigned180(double degrees)
        {
            double normalized = Normalize360(degrees);

            if (normalized > 180.0)
            {
                normalized -= 360.0;
            }

            return normalized;
        }

        /// <summary>
        /// Calcula la diferencia mínima entre dos ángulos.
        /// </summary>
        public static double AngularDifference(double firstDegrees, double secondDegrees)
        {
            return Math.Abs(NormalizeSigned180(firstDegrees - secondDegrees));
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
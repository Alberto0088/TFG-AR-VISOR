/*
 * GeoDistanceCalculator.cs
 * ------------------------------------------------------------
 * Este servicio calcula distancias entre coordenadas geográficas.
 *
 * Utiliza la fórmula de Haversine para obtener la distancia horizontal
 * aproximada entre dos puntos de la Tierra usando latitud y longitud.
 *
 * Se conecta con:
 * - OwnshipGeoState: posición propia del usuario/avión.
 * - AircraftGeoState: posición de cada aeronave detectada.
 * - OpenSkyApiClient: usará este servicio para saber qué aeronave está más cerca.
 */

using System;
using TFG.ARVisor.Domain.Models;

namespace TFG.ARVisor.Domain.Services
{
    public static class GeoDistanceCalculator
    {
        private const double EarthRadiusKm = 6371.0;

        /// <summary>
        /// Calcula la distancia horizontal en kilómetros entre la posición propia y una aeronave.
        /// </summary>
        public static double DistanceKm(OwnshipGeoState ownshipState, AircraftGeoState aircraft)
        {
            if (ownshipState == null)
            {
                throw new ArgumentNullException(nameof(ownshipState));
            }

            if (aircraft == null)
            {
                throw new ArgumentNullException(nameof(aircraft));
            }

            return DistanceKm(
                ownshipState.Latitude,
                ownshipState.Longitude,
                aircraft.Latitude,
                aircraft.Longitude
            );
        }

        /// <summary>
        /// Calcula la distancia horizontal en kilómetros entre dos coordenadas geográficas.
        /// </summary>
        public static double DistanceKm(
            double originLatitude,
            double originLongitude,
            double targetLatitude,
            double targetLongitude)
        {
            double originLatRad = DegreesToRadians(originLatitude);
            double targetLatRad = DegreesToRadians(targetLatitude);

            double deltaLatRad = DegreesToRadians(targetLatitude - originLatitude);
            double deltaLonRad = DegreesToRadians(targetLongitude - originLongitude);

            double a =
                Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                Math.Cos(originLatRad) * Math.Cos(targetLatRad) *
                Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusKm * c;
        }

        /// <summary>
        /// Convierte grados a radianes, que es el formato necesario para las funciones trigonométricas.
        /// </summary>
        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
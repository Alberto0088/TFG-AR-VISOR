/*
 * BoundingBoxBuilder.cs
 * ------------------------------------------------------------
 * Este servicio calcula una caja geográfica alrededor de la posición
 * propia del usuario/avión.
 *
 * Recibe una posición central (latitud y longitud) y un radio en kilómetros.
 * A partir de esos datos genera los límites geográficos necesarios para
 * consultar tráfico aéreo cercano en APIs externas como OpenSky.
 *
 * Se conecta con:
 * - OwnshipGeoState: posición actual del usuario/avión.
 * - GeoBoundingBox: resultado generado.
 * - OpenSkyApiClient: futuro cliente que usará esta caja para consultar aeronaves.
 */

using System;
using TFG.ARVisor.Domain.Models;

namespace TFG.ARVisor.Domain.Services
{
    public static class BoundingBoxBuilder
    {
        private const double KilometersPerLatitudeDegree = 111.32;

        /// <summary>
        /// Crea una bounding box alrededor de la posición propia usando un radio en kilómetros.
        /// </summary>
        public static GeoBoundingBox FromOwnshipPosition(OwnshipGeoState ownshipState, double radiusKm)
        {
            if (ownshipState == null)
            {
                throw new ArgumentNullException(nameof(ownshipState));
            }

            return FromCoordinates(
                ownshipState.Latitude,
                ownshipState.Longitude,
                radiusKm
            );
        }

        /// <summary>
        /// Calcula los límites geográficos mínimos y máximos alrededor de una latitud y longitud.
        /// </summary>
        public static GeoBoundingBox FromCoordinates(double latitude, double longitude, double radiusKm)
        {
            if (radiusKm <= 0)
            {
                throw new ArgumentException("El radio debe ser mayor que 0.", nameof(radiusKm));
            }

            double latitudeDelta = radiusKm / KilometersPerLatitudeDegree;

            double latitudeRadians = latitude * Math.PI / 180.0;
            double longitudeKmPerDegree = KilometersPerLatitudeDegree * Math.Cos(latitudeRadians);

            if (Math.Abs(longitudeKmPerDegree) < 0.0001)
            {
                longitudeKmPerDegree = 0.0001;
            }

            double longitudeDelta = radiusKm / longitudeKmPerDegree;

            double minLatitude = Clamp(latitude - latitudeDelta, -90.0, 90.0);
            double maxLatitude = Clamp(latitude + latitudeDelta, -90.0, 90.0);
            double minLongitude = Clamp(longitude - longitudeDelta, -180.0, 180.0);
            double maxLongitude = Clamp(longitude + longitudeDelta, -180.0, 180.0);

            return new GeoBoundingBox(
                minLatitude,
                minLongitude,
                maxLatitude,
                maxLongitude
            );
        }

        /// <summary>
        /// Limita un valor entre un mínimo y un máximo para evitar coordenadas fuera de rango.
        /// </summary>
        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
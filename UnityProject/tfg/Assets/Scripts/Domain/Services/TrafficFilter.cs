/*
 * TrafficFilter.cs
 * ------------------------------------------------------------
 * Este servicio filtra las aeronaves recibidas desde OpenSky para
 * quedarse únicamente con las que son realmente relevantes para el sistema.
 *
 * OpenSky recibe una bounding box rectangular, por lo que puede devolver
 * aeronaves que están dentro de esa caja pero fuera del radio circular
 * configurado por el usuario.
 *
 * Este filtro corrige ese comportamiento calculando la distancia real entre
 * la posición propia y cada aeronave.
 *
 * Se conecta con:
 * - OwnshipGeoState: posición actual del usuario/avión.
 * - AircraftGeoState: aeronaves recibidas desde OpenSky.
 * - GeoDistanceCalculator: calcula la distancia real en kilómetros.
 * - OpenSkyApiClient: usará este filtro antes de actualizar el HUD.
 */

using System;
using System.Collections.Generic;
using TFG.ARVisor.Domain.Models;

namespace TFG.ARVisor.Domain.Services
{
    public static class TrafficFilter
    {
        /// <summary>
        /// Devuelve únicamente las aeronaves que están dentro del radio máximo indicado.
        /// </summary>
        public static List<AircraftGeoState> FilterByDistance(
            OwnshipGeoState ownshipState,
            List<AircraftGeoState> aircraft,
            double maxDistanceKm)
        {
            if (ownshipState == null)
            {
                throw new ArgumentNullException(nameof(ownshipState));
            }

            if (aircraft == null)
            {
                return new List<AircraftGeoState>();
            }

            if (maxDistanceKm <= 0)
            {
                throw new ArgumentException("El radio máximo debe ser mayor que 0.", nameof(maxDistanceKm));
            }

            List<AircraftGeoState> filteredAircraft = new List<AircraftGeoState>();

            foreach (AircraftGeoState item in aircraft)
            {
                double distanceKm = GeoDistanceCalculator.DistanceKm(ownshipState, item);

                if (distanceKm <= maxDistanceKm)
                {
                    filteredAircraft.Add(item);
                }
            }

            return filteredAircraft;
        }
    }
}
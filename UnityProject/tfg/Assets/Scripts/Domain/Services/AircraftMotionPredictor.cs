/*
 * AircraftMotionPredictor.cs
 * ------------------------------------------------------------
 * Predice la posición aproximada de una aeronave entre actualizaciones externas.
 *
 * Usa dead reckoning:
 * - última latitud/longitud conocida,
 * - velocidad,
 * - rumbo,
 * - tiempo transcurrido desde el último contacto.
 *
 * No sustituye datos reales, solo mejora la continuidad visual del HUD entre
 * actualizaciones de OpenSky o escenarios simulados.
 */

using System;
using System.Collections.Generic;
using TFG.ARVisor.Domain.Models;

namespace TFG.ARVisor.Domain.Services
{
    public static class AircraftMotionPredictor
    {
        /// <summary>
        /// Predice una lista de aeronaves usando la hora actual.
        /// </summary>
        public static List<AircraftGeoState> PredictAircraftPositions(
            List<AircraftGeoState> aircraft,
            double maxPredictionSeconds = 120.0)
        {
            List<AircraftGeoState> predictedAircraft = new List<AircraftGeoState>();

            if (aircraft == null)
            {
                return predictedAircraft;
            }

            long currentUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            foreach (AircraftGeoState item in aircraft)
            {
                AircraftGeoState predicted = PredictAircraftPosition(
                    item,
                    currentUtc,
                    maxPredictionSeconds
                );

                if (predicted != null)
                {
                    predictedAircraft.Add(predicted);
                }
            }

            return predictedAircraft;
        }

        /// <summary>
        /// Predice la posición actual de una aeronave concreta.
        /// </summary>
        private static AircraftGeoState PredictAircraftPosition(
            AircraftGeoState aircraft,
            long currentUtc,
            double maxPredictionSeconds)
        {
            if (aircraft == null)
            {
                return null;
            }

            if (!aircraft.VelocityMps.HasValue || !aircraft.HeadingDegrees.HasValue)
            {
                return aircraft;
            }

            double elapsedSeconds = currentUtc - aircraft.LastContactUtc;

            if (elapsedSeconds <= 0.0)
            {
                return aircraft;
            }

            elapsedSeconds = Math.Min(elapsedSeconds, maxPredictionSeconds);

            double travelledMeters = aircraft.VelocityMps.Value * elapsedSeconds;

            GeoProjectionCalculator.ProjectFromLocalOffset(
                aircraft.Latitude,
                aircraft.Longitude,
                aircraft.HeadingDegrees.Value,
                forwardMeters: travelledMeters,
                rightMeters: 0.0,
                out double predictedLatitude,
                out double predictedLongitude
            );

            return new AircraftGeoState(
                aircraft.Id,
                aircraft.Callsign,
                aircraft.OriginCountry,
                predictedLatitude,
                predictedLongitude,
                aircraft.AltitudeMeters,
                aircraft.VelocityMps,
                aircraft.HeadingDegrees,
                aircraft.VerticalRateMps,
                aircraft.LastContactUtc
            );
        }
    }
}
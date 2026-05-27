/*
 * AircraftTargetSelector.cs
 * ------------------------------------------------------------
 * Este servicio selecciona qué aeronave debe mostrarse como TARGET en el HUD.
 *
 * La selección no cambia la posición real de la aeronave ni el rumbo del avión.
 * Solo decide qué aeronave resulta más interesante mostrar según la zona hacia
 * la que está mirando el piloto.
 *
 * Funcionamiento general:
 * 1. Calcula el bearing desde la posición propia hasta cada aeronave.
 * 2. Convierte ese bearing a posición relativa respecto al rumbo del avión.
 * 3. Compara esa posición relativa con la orientación actual de la cabeza.
 * 4. Si hay aeronaves dentro de la zona mirada, elige la mejor.
 * 5. Si no hay ninguna en esa zona, usa como fallback la aeronave más cercana.
 *
 * Se conecta con:
 * - GeoBearingCalculator: calcula bearing y diferencias angulares.
 * - GeoDistanceCalculator: calcula distancia real.
 * - OpenSkyApiClient: usa este selector para decidir qué TARGET mostrar.
 */

using System;
using System.Collections.Generic;
using TFG.ARVisor.Domain.Models;

namespace TFG.ARVisor.Domain.Services
{
    public static class AircraftTargetSelector
    {
        /// <summary>
        /// Selecciona la aeronave más relevante según la dirección actual de mirada del piloto.
        /// </summary>
        public static AircraftTargetSelectionResult SelectByHeadsetDirection(
            OwnshipGeoState ownshipState,
            List<AircraftGeoState> aircraft,
            double aircraftHeadingDegrees,
            double headRelativeYawDegrees,
            double maxViewAngleDifferenceDegrees)
        {
            if (ownshipState == null)
            {
                throw new ArgumentNullException(nameof(ownshipState));
            }

            if (aircraft == null || aircraft.Count == 0)
            {
                return Empty();
            }

            AircraftTargetSelectionResult closestFallback = null;
            double closestDistanceKm = double.MaxValue;

            AircraftTargetSelectionResult bestViewTarget = null;
            double bestViewScore = double.MaxValue;

            foreach (AircraftGeoState item in aircraft)
            {
                double distanceKm = GeoDistanceCalculator.DistanceKm(ownshipState, item);
                double bearingDegrees = GeoBearingCalculator.BearingDegrees(ownshipState, item);

                double relativeBearingDegrees = GeoBearingCalculator.NormalizeSigned180(
                    bearingDegrees - aircraftHeadingDegrees
                );

                double viewAngleDifferenceDegrees = GeoBearingCalculator.AngularDifference(
                    relativeBearingDegrees,
                    headRelativeYawDegrees
                );

                AircraftTargetSelectionResult current = new AircraftTargetSelectionResult(
                    item,
                    distanceKm,
                    bearingDegrees,
                    relativeBearingDegrees,
                    headRelativeYawDegrees,
                    viewAngleDifferenceDegrees,
                    viewAngleDifferenceDegrees <= maxViewAngleDifferenceDegrees
                );

                if (distanceKm < closestDistanceKm)
                {
                    closestDistanceKm = distanceKm;
                    closestFallback = current;
                }

                if (viewAngleDifferenceDegrees <= maxViewAngleDifferenceDegrees)
                {
                    double score = CalculateScore(distanceKm, viewAngleDifferenceDegrees);

                    if (score < bestViewScore)
                    {
                        bestViewScore = score;
                        bestViewTarget = current;
                    }
                }
            }

            if (bestViewTarget != null)
            {
                return bestViewTarget;
            }

            return closestFallback ?? Empty();
        }

        /// <summary>
        /// Crea un resultado vacío cuando no hay aeronaves disponibles.
        /// </summary>
        public static AircraftTargetSelectionResult Empty()
        {
            return new AircraftTargetSelectionResult(
                null,
                null,
                null,
                null,
                null,
                null,
                false
            );
        }

        /// <summary>
        /// Calcula una puntuación simple combinando distancia y alineación con la mirada.
        /// Cuanto menor sea la puntuación, más relevante será la aeronave.
        /// </summary>
        private static double CalculateScore(double distanceKm, double viewAngleDifferenceDegrees)
        {
            return viewAngleDifferenceDegrees + distanceKm * 0.15;
        }
    }
}
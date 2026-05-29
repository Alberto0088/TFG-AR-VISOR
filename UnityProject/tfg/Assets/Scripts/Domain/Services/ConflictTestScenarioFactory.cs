/*
 * ConflictTestScenarioFactory.cs
 * ------------------------------------------------------------
 * Genera aeronaves simuladas para probar el motor de predicción de conflicto.
 *
 * Escenarios incluidos:
 * - SafeParallel: aeronave separada y no conflictiva.
 * - CrossingTraffic: aeronave cruzando la trayectoria con riesgo medio.
 * - HeadOnConflict: aeronave frontal con riesgo alto.
 *
 * Estos escenarios son de desarrollo y validación. No sustituyen al tráfico
 * real recibido desde OpenSky, pero permiten demostrar que el motor CPA/TCPA
 * responde correctamente ante situaciones controladas.
 *
 * Se conecta con:
 * - ConflictTestScenarioType: selección del escenario.
 * - ConflictPredictionEngine: evalúa la aeronave generada.
 * - OpenSkyApiClient: integra el escenario en el flujo de tráfico.
 */

using System;
using System.Collections.Generic;
using TFG.ARVisor.Domain.Models;

namespace TFG.ARVisor.Domain.Services
{
    public static class ConflictTestScenarioFactory
    {
        /// <summary>
        /// Crea una lista con una aeronave simulada según el escenario seleccionado.
        /// </summary>
        public static List<AircraftGeoState> CreateScenarioAircraft(
            OwnshipGeoState ownshipState,
            OwnshipMotionState ownshipMotion,
            ConflictTestScenarioType scenarioType)
        {
            List<AircraftGeoState> aircraft = new List<AircraftGeoState>();

            if (ownshipState == null)
            {
                return aircraft;
            }

            double referenceTrack = GetReferenceTrack(ownshipMotion);
            double ownshipAltitude = ownshipState.AltitudeMeters ?? 1000.0;

            switch (scenarioType)
            {
                case ConflictTestScenarioType.HeadOnConflict:
                    aircraft.Add(CreateHeadOnConflict(ownshipState, referenceTrack, ownshipAltitude));
                    break;

                case ConflictTestScenarioType.CrossingTraffic:
                    aircraft.Add(CreateCrossingTraffic(ownshipState, referenceTrack, ownshipAltitude));
                    break;

                case ConflictTestScenarioType.SafeParallel:
                default:
                    aircraft.Add(CreateSafeParallel(ownshipState, referenceTrack, ownshipAltitude));
                    break;
            }

            return aircraft;
        }

        /// <summary>
        /// Obtiene el rumbo propio estimado. Si no hay movimiento fiable, usa 90 grados como referencia.
        /// </summary>
        private static double GetReferenceTrack(OwnshipMotionState ownshipMotion)
        {
            if (ownshipMotion != null &&
                ownshipMotion.HasReliableMotion &&
                ownshipMotion.TrackDegrees.HasValue)
            {
                return ownshipMotion.TrackDegrees.Value;
            }

            return 90.0;
        }

        /// <summary>
        /// Crea una aeronave separada y no conflictiva.
        /// Resultado esperado: LOW / PATH CLEAR.
        /// </summary>
        private static AircraftGeoState CreateSafeParallel(
            OwnshipGeoState ownshipState,
            double referenceTrack,
            double ownshipAltitude)
        {
            GeoProjectionCalculator.ProjectFromLocalOffset(
                ownshipState.Latitude,
                ownshipState.Longitude,
                referenceTrack,
                forwardMeters: 60000.0,
                rightMeters: 60000.0,
                out double latitude,
                out double longitude
            );

            return CreateAircraft(
                id: "test_safe_parallel",
                callsign: "TESTSAFE",
                latitude: latitude,
                longitude: longitude,
                altitudeMeters: ownshipAltitude + 300.0,
                velocityMps: 40.0,
                headingDegrees: GeoBearingCalculator.Normalize360(referenceTrack + 10.0)
            );
        }

        /// <summary>
        /// Crea una aeronave que cruza la trayectoria propia con separación moderada.
        /// Resultado esperado: MED / TRAJECTORY WATCH.
        /// </summary>
        private static AircraftGeoState CreateCrossingTraffic(
            OwnshipGeoState ownshipState,
            double referenceTrack,
            double ownshipAltitude)
        {
            GeoProjectionCalculator.ProjectFromLocalOffset(
                ownshipState.Latitude,
                ownshipState.Longitude,
                referenceTrack,
                forwardMeters: 19000.0,
                rightMeters: 9000.0,
                out double latitude,
                out double longitude
            );

            return CreateAircraft(
                id: "test_crossing_traffic",
                callsign: "TESTMED",
                latitude: latitude,
                longitude: longitude,
                altitudeMeters: ownshipAltitude + 300.0,
                velocityMps: 45.0,
                headingDegrees: GeoBearingCalculator.Normalize360(referenceTrack - 90.0)
            );
        }

        /// <summary>
        /// Crea una aeronave frontal en sentido contrario.
        /// Resultado esperado: HIGH / CONFLICT RISK.
        /// </summary>
        private static AircraftGeoState CreateHeadOnConflict(
            OwnshipGeoState ownshipState,
            double referenceTrack,
            double ownshipAltitude)
        {
            GeoProjectionCalculator.ProjectFromLocalOffset(
                ownshipState.Latitude,
                ownshipState.Longitude,
                referenceTrack,
                forwardMeters: 20000.0,
                rightMeters: 0.0,
                out double latitude,
                out double longitude
            );

            return CreateAircraft(
                id: "test_head_on_conflict",
                callsign: "TESTHIGH",
                latitude: latitude,
                longitude: longitude,
                altitudeMeters: ownshipAltitude + 200.0,
                velocityMps: 70.0,
                headingDegrees: GeoBearingCalculator.Normalize360(referenceTrack + 180.0)
            );
        }

        /// <summary>
        /// Crea una aeronave simulada compatible con el modelo interno AircraftGeoState.
        /// </summary>
        private static AircraftGeoState CreateAircraft(
            string id,
            string callsign,
            double latitude,
            double longitude,
            double altitudeMeters,
            double velocityMps,
            double headingDegrees)
        {
            return new AircraftGeoState(
                id,
                callsign,
                "Test Scenario",
                latitude,
                longitude,
                altitudeMeters,
                velocityMps,
                headingDegrees,
                verticalRateMps: 0.0,
                lastContactUtc: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            );
        }
    }
}
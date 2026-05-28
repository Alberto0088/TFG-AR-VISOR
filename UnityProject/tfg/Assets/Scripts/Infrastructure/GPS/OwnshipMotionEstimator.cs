/*
 * OwnshipMotionEstimator.cs
 * ------------------------------------------------------------
 * Este script estima el rumbo y la velocidad propios a partir del historial
 * reciente de posiciones GPS.
 *
 * Funcionamiento general:
 * 1. Lee periódicamente la posición actual desde ExternalGpsProvider.
 * 2. Guarda muestras de posición durante una ventana temporal.
 * 3. Compara la muestra más antigua útil con la más reciente.
 * 4. Calcula distancia recorrida, tiempo transcurrido, velocidad y rumbo.
 * 5. Si el movimiento no es suficiente, marca la estimación como no fiable.
 *
 * Además, incluye un modo de simulación para desarrollo:
 * - toma la posición GPS actual como punto de referencia,
 * - simula un rumbo y velocidad propios,
 * - permite probar CPA/TCPA sin tener que moverse físicamente.
 *
 * Se conecta con:
 * - ExternalGpsProvider: fuente de posición propia.
 * - OwnshipMotionSample: historial de posiciones.
 * - OwnshipMotionState: resultado estimado.
 * - GeoBearingCalculator: cálculo del rumbo.
 * - GeoDistanceCalculator: cálculo de distancia recorrida.
 */

using System;
using System.Collections.Generic;
using TFG.ARVisor.Domain.Models;
using TFG.ARVisor.Domain.Services;
using UnityEngine;

namespace TFG.ARVisor.Infrastructure.Gps
{
    public class OwnshipMotionEstimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ExternalGpsProvider gpsProvider;

        [Header("Sampling Settings")]
        [SerializeField] private float sampleIntervalSeconds = 1f;
        [SerializeField] private double historyWindowSeconds = 120.0;

        [Header("Reliability Settings")]
        [SerializeField] private double minimumTimeDeltaSeconds = 10.0;
        [SerializeField] private double minimumDistanceMeters = 20.0;
        [SerializeField] private double minimumSpeedMps = 1.0;

        [Header("Debug Motion Simulation")]
        [SerializeField] private bool useDebugMotion = false;
        [SerializeField] private double debugTrackDegrees = 90.0;
        [SerializeField] private double debugSpeedMps = 45.0;

        [Header("Debug")]
        [SerializeField] private bool logMotionToConsole = false;

        private readonly List<OwnshipMotionSample> samples = new List<OwnshipMotionSample>();

        private float nextSampleTime;
        private OwnshipMotionState currentMotionState;

        private bool hasDebugOrigin;
        private double debugOriginLatitude;
        private double debugOriginLongitude;
        private DateTime debugStartTimeUtc;

        public OwnshipMotionState CurrentMotionState => currentMotionState;

        /// <summary>
        /// Inicializa el estado de movimiento como no fiable hasta tener suficientes muestras GPS.
        /// </summary>
        private void Start()
        {
            currentMotionState = OwnshipMotionState.NotReliable(
                "Waiting for GPS movement history.",
                null
            );
        }

        /// <summary>
        /// Toma muestras periódicas del GPS y actualiza la estimación de rumbo y velocidad.
        /// </summary>
        private void Update()
        {
            if (Time.time < nextSampleTime)
            {
                return;
            }

            nextSampleTime = Time.time + sampleIntervalSeconds;

            CaptureGpsSample();

            if (useDebugMotion)
            {
                EstimateDebugMotion();
                return;
            }

            RemoveOldSamples();
            EstimateMotion();
        }

        /// <summary>
        /// Captura una muestra de posición propia desde el proveedor GPS.
        /// </summary>
        private void CaptureGpsSample()
        {
            if (gpsProvider == null || gpsProvider.CurrentState == null)
            {
                currentMotionState = OwnshipMotionState.NotReliable(
                    "GPS position not available.",
                    null
                );

                return;
            }

            OwnshipGeoState currentState = gpsProvider.CurrentState;

            OwnshipMotionSample sample = new OwnshipMotionSample(
                currentState.Latitude,
                currentState.Longitude,
                currentState.AltitudeMeters,
                DateTime.UtcNow
            );

            samples.Add(sample);

            if (!hasDebugOrigin)
            {
                hasDebugOrigin = true;
                debugOriginLatitude = currentState.Latitude;
                debugOriginLongitude = currentState.Longitude;
                debugStartTimeUtc = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Genera un movimiento propio simulado para probar la predicción de conflicto sin desplazamiento real.
        /// </summary>
        /// <summary>
        /// Genera un movimiento propio simulado para probar la predicción de conflicto sin desplazamiento real.
        /// Si hay GPS disponible, usa su altitud. Si no lo hay, simula igualmente rumbo y velocidad.
        ///</summary>
        private void EstimateDebugMotion()
        {
            double? altitudeMeters = null;

            if (gpsProvider != null && gpsProvider.CurrentState != null)
            {
                altitudeMeters = gpsProvider.CurrentState.AltitudeMeters;
            }

            currentMotionState = new OwnshipMotionState(
                true,
                GeoBearingCalculator.Normalize360(debugTrackDegrees),
                debugSpeedMps,
                altitudeMeters,
                "Debug simulated ownship motion."
            );

            if (logMotionToConsole)
            {
                double elapsedSeconds = hasDebugOrigin
                    ? (DateTime.UtcNow - debugStartTimeUtc).TotalSeconds
                    : 0.0;

                double simulatedDistanceMeters = debugSpeedMps * elapsedSeconds;

                Debug.Log(
                    $"Ownship motion DEBUG -> " +
                    $"Track: {debugTrackDegrees:0.0}°, " +
                    $"Speed: {debugSpeedMps:0.0} m/s, " +
                    $"SimDistance: {simulatedDistanceMeters:0.0} m"
                );
            }
        }

        /// <summary>
        /// Elimina muestras antiguas fuera de la ventana temporal configurada.
        /// </summary>
        private void RemoveOldSamples()
        {
            DateTime limit = DateTime.UtcNow.AddSeconds(-historyWindowSeconds);

            samples.RemoveAll(sample => sample.TimestampUtc < limit);
        }

        /// <summary>
        /// Estima rumbo y velocidad propios usando la muestra más antigua y la más reciente.
        /// </summary>
        private void EstimateMotion()
        {
            if (samples.Count < 2)
            {
                currentMotionState = OwnshipMotionState.NotReliable(
                    "Not enough GPS samples.",
                    GetLatestAltitude()
                );

                return;
            }

            OwnshipMotionSample oldest = samples[0];
            OwnshipMotionSample latest = samples[samples.Count - 1];

            double deltaSeconds = (latest.TimestampUtc - oldest.TimestampUtc).TotalSeconds;

            if (deltaSeconds < minimumTimeDeltaSeconds)
            {
                currentMotionState = OwnshipMotionState.NotReliable(
                    "Not enough time between GPS samples.",
                    latest.AltitudeMeters
                );

                return;
            }

            double distanceKm = GeoDistanceCalculator.DistanceKm(
                oldest.Latitude,
                oldest.Longitude,
                latest.Latitude,
                latest.Longitude
            );

            double distanceMeters = distanceKm * 1000.0;

            if (distanceMeters < minimumDistanceMeters)
            {
                currentMotionState = OwnshipMotionState.NotReliable(
                    "GPS movement is too small to estimate reliable heading.",
                    latest.AltitudeMeters
                );

                return;
            }

            double speedMps = distanceMeters / deltaSeconds;

            if (speedMps < minimumSpeedMps)
            {
                currentMotionState = OwnshipMotionState.NotReliable(
                    "Estimated speed is too low.",
                    latest.AltitudeMeters
                );

                return;
            }

            double trackDegrees = GeoBearingCalculator.BearingDegrees(
                oldest.Latitude,
                oldest.Longitude,
                latest.Latitude,
                latest.Longitude
            );

            currentMotionState = new OwnshipMotionState(
                true,
                trackDegrees,
                speedMps,
                latest.AltitudeMeters,
                "Reliable GPS motion."
            );

            if (logMotionToConsole)
            {
                Debug.Log(
                    $"Ownship motion -> " +
                    $"Track: {trackDegrees:0.0}°, " +
                    $"Speed: {speedMps:0.0} m/s, " +
                    $"Samples: {samples.Count}, " +
                    $"Distance: {distanceMeters:0.0} m"
                );
            }
        }

        /// <summary>
        /// Devuelve la última altitud conocida si existe.
        /// </summary>
        private double? GetLatestAltitude()
        {
            if (samples.Count == 0)
            {
                return null;
            }

            return samples[samples.Count - 1].AltitudeMeters;
        }
    }
}
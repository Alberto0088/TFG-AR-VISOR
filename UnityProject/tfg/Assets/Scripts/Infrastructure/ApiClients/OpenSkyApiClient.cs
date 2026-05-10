/*
 * OpenSkyApiClient.cs
 * ------------------------------------------------------------
 * Este script realiza la conexión con la API de OpenSky para obtener
 * tráfico aéreo actual cerca de la posición propia del usuario/avión.
 *
 * Funcionamiento general:
 * 1. Obtiene la posición actual desde ExternalGpsProvider.
 * 2. Calcula una bounding box alrededor de esa posición.
 * 3. Construye la URL de consulta para OpenSky.
 * 4. Realiza una petición HTTP real.
 * 5. Convierte el JSON recibido en aeronaves internas mediante OpenSkyParser.
 * 6. Calcula la distancia de cada aeronave respecto a la posición propia.
 * 7. Actualiza el HUD con el número de aeronaves y la distancia de la más cercana.
 *
 * Se conecta con:
 * - ExternalGpsProvider: obtiene la posición propia.
 * - BoundingBoxBuilder: genera la zona de búsqueda.
 * - OpenSkyParser: convierte el JSON de OpenSky en AircraftGeoState.
 * - GeoDistanceCalculator: calcula distancia entre usuario y aeronaves.
 * - HudController: muestra el resumen de tráfico real en el visor.
 */

using System.Collections;
using System.Collections.Generic;
using TFG.ARVisor.Domain.Models;
using TFG.ARVisor.Domain.Services;
using TFG.ARVisor.Infrastructure.Gps;
using TFG.ARVisor.Presentation.HUD;
using UnityEngine;
using UnityEngine.Networking;

namespace TFG.ARVisor.Infrastructure.ApiClients
{
    public class OpenSkyApiClient : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ExternalGpsProvider gpsProvider;
        [SerializeField] private HudController hudController;

        [Header("OpenSky Settings")]
        [SerializeField] private string openSkyBaseUrl = "https://opensky-network.org/api/states/all";
        [SerializeField] private double searchRadiusKm = 100.0;

        [Header("Request Settings")]
        [SerializeField] private float refreshSeconds = 15f;
        [SerializeField] private int requestTimeoutSeconds = 10;

        [Header("Debug Settings")]
        [SerializeField] private bool logUrlToConsole = true;
        [SerializeField] private bool logRawJsonPreview = false;

        private Coroutine pollingCoroutine;

        /// <summary>
        /// Inicia el ciclo de consultas a OpenSky cuando arranca la escena.
        /// </summary>
        private void Start()
        {
            pollingCoroutine = StartCoroutine(PollOpenSkyLoop());
        }

        /// <summary>
        /// Detiene el ciclo de consultas si el objeto se desactiva.
        /// </summary>
        private void OnDisable()
        {
            if (pollingCoroutine != null)
            {
                StopCoroutine(pollingCoroutine);
                pollingCoroutine = null;
            }
        }

        /// <summary>
        /// Ejecuta consultas periódicas a OpenSky cada cierto número de segundos.
        /// </summary>
        private IEnumerator PollOpenSkyLoop()
        {
            while (true)
            {
                yield return FetchCurrentTraffic();
                yield return new WaitForSeconds(refreshSeconds);
            }
        }

        /// <summary>
        /// Obtiene la posición propia, genera la bounding box y lanza la petición HTTP a OpenSky.
        /// </summary>
        private IEnumerator FetchCurrentTraffic()
        {
            if (gpsProvider == null)
            {
                Debug.LogWarning("OpenSkyApiClient: ExternalGpsProvider reference is missing.");
                yield break;
            }

            OwnshipGeoState ownshipState = gpsProvider.CurrentState;

            if (ownshipState == null)
            {
                Debug.LogWarning("OpenSkyApiClient: waiting for GPS position before requesting OpenSky.");
                yield break;
            }

            GeoBoundingBox boundingBox = BoundingBoxBuilder.FromOwnshipPosition(
                ownshipState,
                searchRadiusKm
            );

            string requestUrl = $"{openSkyBaseUrl}?{boundingBox.ToOpenSkyQuery()}";

            if (logUrlToConsole)
            {
                Debug.Log($"OpenSky request URL -> {requestUrl}");
            }

            using UnityWebRequest request = UnityWebRequest.Get(requestUrl);
            request.timeout = requestTimeoutSeconds;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"OpenSky request failed: {request.responseCode} - {request.error}");
                UpdateHudWithTraffic(0, "--");
                yield break;
            }

            string json = request.downloadHandler.text;

            ProcessOpenSkyResponse(json, ownshipState);
        }

        /// <summary>
        /// Procesa el JSON recibido desde OpenSky, convierte las aeronaves a modelos internos
        /// y actualiza el HUD con el número de aeronaves reales y la distancia de la más cercana.
        /// </summary>
        private void ProcessOpenSkyResponse(string json, OwnshipGeoState ownshipState)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("OpenSky response is empty.");
                UpdateHudWithTraffic(0, "--");
                return;
            }

            List<AircraftGeoState> aircraft = OpenSkyParser.ParseAircraft(json);

            List<AircraftGeoState> nearbyAircraft = TrafficFilter.FilterByDistance(
                ownshipState,
                aircraft,
                searchRadiusKm
            );

            Debug.Log(
                $"OpenSky aircraft parsed: {aircraft.Count}. " +
                $"Inside {searchRadiusKm:0} KM: {nearbyAircraft.Count}"
            );

            double? nearestDistanceKm = GetNearestAircraftDistanceKm(ownshipState, nearbyAircraft);

            string nearestDistanceText = nearestDistanceKm.HasValue
                ? $"{nearestDistanceKm.Value:0.0} KM"
                : "--";

            UpdateHudWithTraffic(nearbyAircraft.Count, nearestDistanceText);

            LogAircraftList(ownshipState, nearbyAircraft);

            if (logRawJsonPreview)
            {
                int previewLength = Mathf.Min(json.Length, 600);
                Debug.Log($"OpenSky JSON preview -> {json.Substring(0, previewLength)}");
            }
        }

        /// <summary>
        /// Muestra en consola las aeronaves detectadas con sus datos principales y distancia.
        /// </summary>
        private void LogAircraftList(OwnshipGeoState ownshipState, List<AircraftGeoState> aircraft)
        {
            foreach (AircraftGeoState item in aircraft)
            {
                double distanceKm = GeoDistanceCalculator.DistanceKm(ownshipState, item);

                Debug.Log(
                    $"Aircraft -> " +
                    $"ID: {item.Id}, " +
                    $"Callsign: {item.Callsign}, " +
                    $"Country: {item.OriginCountry}, " +
                    $"Distance: {distanceKm:0.0} KM, " +
                    $"Lat: {item.Latitude}, " +
                    $"Lon: {item.Longitude}, " +
                    $"Alt: {item.AltitudeMeters}, " +
                    $"Vel: {item.VelocityMps}, " +
                    $"Heading: {item.HeadingDegrees}"
                );
            }
        }

        /// <summary>
        /// Busca la aeronave más cercana a la posición propia y devuelve su distancia en kilómetros.
        /// </summary>
        private double? GetNearestAircraftDistanceKm(
            OwnshipGeoState ownshipState,
            List<AircraftGeoState> aircraft)
        {
            if (ownshipState == null || aircraft == null || aircraft.Count == 0)
            {
                return null;
            }

            double nearestDistanceKm = double.MaxValue;

            foreach (AircraftGeoState item in aircraft)
            {
                double distanceKm = GeoDistanceCalculator.DistanceKm(ownshipState, item);

                if (distanceKm < nearestDistanceKm)
                {
                    nearestDistanceKm = distanceKm;
                }
            }

            return nearestDistanceKm == double.MaxValue
                ? null
                : nearestDistanceKm;
        }

        /// <summary>
        /// Actualiza el HUD con el resumen básico de tráfico real recibido desde OpenSky.
        /// </summary>
        private void UpdateHudWithTraffic(int aircraftCount, string nearestDistanceText)
        {
            if (hudController == null)
            {
                return;
            }

            TrafficSnapshot snapshot = new TrafficSnapshot(
                nearbyAircraft: aircraftCount,
                nearestDistance: nearestDistanceText,
                riskLevel: RiskLevel.Low,
                alertMessage: aircraftCount > 0 ? "TRAFFIC DETECTED" : "NO ALERTS"
            );

            hudController.RenderTraffic(snapshot);
        }
    }
}
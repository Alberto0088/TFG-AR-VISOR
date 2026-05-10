/*
 * OpenSkyApiClient.cs
 * ------------------------------------------------------------
 * Este script realiza la primera conexión real con la API de OpenSky.
 *
 * Su función es:
 * 1. Obtener la posición actual del usuario/avión desde ExternalGpsProvider.
 * 2. Calcular una bounding box alrededor de esa posición.
 * 3. Construir la URL de consulta para OpenSky.
 * 4. Realizar una petición HTTP real.
 * 5. Mostrar en consola si se ha recibido JSON correctamente.
 *
 * De momento NO interpreta todavía todos los datos de las aeronaves.
 * Este script solo valida que Unity puede conectarse a OpenSky y recibir
 * tráfico aéreo actual dentro de la zona calculada.
 *
 * Se conecta con:
 * - ExternalGpsProvider: obtiene la posición propia.
 * - BoundingBoxBuilder: genera la zona de búsqueda.
 * - GeoBoundingBox: contiene los límites de la consulta.
 * - OpenSkyParser: se añadirá después para convertir el JSON en modelos propios.
 */

using System.Collections;
using TFG.ARVisor.Domain.Models;
using TFG.ARVisor.Domain.Services;
using TFG.ARVisor.Infrastructure.Gps;
using UnityEngine;
using UnityEngine.Networking;

namespace TFG.ARVisor.Infrastructure.ApiClients
{
    public class OpenSkyApiClient : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ExternalGpsProvider gpsProvider;

        [Header("OpenSky Settings")]
        [SerializeField] private string openSkyBaseUrl = "https://opensky-network.org/api/states/all";
        [SerializeField] private double searchRadiusKm = 50.0;

        [Header("Request Settings")]
        [SerializeField] private float refreshSeconds = 15f;
        [SerializeField] private int requestTimeoutSeconds = 10;

        [Header("Debug Settings")]
        [SerializeField] private bool logUrlToConsole = true;
        [SerializeField] private bool logRawJsonPreview = true;

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
                yield break;
            }

            string json = request.downloadHandler.text;

            LogOpenSkyResponse(json);
        }

        /// <summary>
        /// Muestra información básica del JSON recibido para confirmar que la conexión funciona.
        /// </summary>
        private void LogOpenSkyResponse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("OpenSky response is empty.");
                return;
            }

            int estimatedAircraftCount = EstimateAircraftCount(json);

            Debug.Log(
                $"OpenSky response received. JSON length: {json.Length} chars. " +
                $"Estimated aircraft count: {estimatedAircraftCount}"
            );

            if (logRawJsonPreview)
            {
                int previewLength = Mathf.Min(json.Length, 600);
                Debug.Log($"OpenSky JSON preview -> {json.Substring(0, previewLength)}");
            }
        }

        /// <summary>
        /// Estima de forma provisional cuántas aeronaves vienen en el JSON.
        /// El parser real se implementará después, porque OpenSky devuelve arrays internos complejos.
        /// </summary>
        private int EstimateAircraftCount(string json)
        {
            if (!json.Contains("\"states\"") || json.Contains("\"states\":null"))
            {
                return 0;
            }

            int statesStart = json.IndexOf("\"states\":[[", System.StringComparison.Ordinal);

            if (statesStart < 0)
            {
                return 0;
            }

            int count = 1;
            int searchIndex = statesStart;

            while (true)
            {
                int nextIndex = json.IndexOf("],[", searchIndex, System.StringComparison.Ordinal);

                if (nextIndex < 0)
                {
                    break;
                }

                count++;
                searchIndex = nextIndex + 3;
            }

            return count;
        }
    }
}
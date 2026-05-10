/*
 * OpenSkyQueryPreview.cs
 * ------------------------------------------------------------
 * Este script genera una URL de consulta para OpenSky a partir
 * de la posición propia recibida por el ExternalGpsProvider.
 *
 * No realiza todavía la llamada real a la API. Su función es
 * comprobar que el sistema ya puede:
 *
 * 1. Leer la posición actual del usuario/avión.
 * 2. Calcular una bounding box alrededor de esa posición.
 * 3. Construir una URL válida para consultar tráfico aéreo cercano.
 *
 * Se conecta con:
 * - ExternalGpsProvider: obtiene la posición propia actual.
 * - BoundingBoxBuilder: genera la caja geográfica de búsqueda.
 * - GeoBoundingBox: almacena los límites geográficos.
 *
 * Este script es temporal/de apoyo y sirve para validar el flujo
 * antes de implementar el cliente real de OpenSky.
 */

using TFG.ARVisor.Domain.Models;
using TFG.ARVisor.Domain.Services;
using TFG.ARVisor.Infrastructure.Gps;
using UnityEngine;

namespace TFG.ARVisor.Infrastructure.ApiClients
{
    public class OpenSkyQueryPreview : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ExternalGpsProvider gpsProvider;

        [Header("OpenSky Settings")]
        [SerializeField] private string openSkyBaseUrl = "https://opensky-network.org/api/states/all";
        [SerializeField] private double searchRadiusKm = 50.0;

        [Header("Debug Settings")]
        [SerializeField] private float refreshSeconds = 3f;
        [SerializeField] private bool logToConsole = true;

        private float timer;

        /// <summary>
        /// Lanza una primera generación de URL al iniciar la escena.
        /// </summary>
        private void Start()
        {
            BuildAndLogOpenSkyUrl();
        }

        /// <summary>
        /// Regenera la URL cada cierto tiempo para comprobar que se actualiza con la posición actual.
        /// </summary>
        private void Update()
        {
            timer += Time.deltaTime;

            if (timer >= refreshSeconds)
            {
                timer = 0f;
                BuildAndLogOpenSkyUrl();
            }
        }

        /// <summary>
        /// Obtiene la posición propia, calcula la bounding box y construye la URL de consulta para OpenSky.
        /// </summary>
        private void BuildAndLogOpenSkyUrl()
        {
            if (gpsProvider == null)
            {
                Debug.LogWarning("OpenSkyQueryPreview: ExternalGpsProvider reference is missing.");
                return;
            }

            OwnshipGeoState ownshipState = gpsProvider.CurrentState;

            if (ownshipState == null)
            {
                Debug.LogWarning("OpenSkyQueryPreview: waiting for GPS position...");
                return;
            }

            GeoBoundingBox boundingBox = BoundingBoxBuilder.FromOwnshipPosition(
                ownshipState,
                searchRadiusKm
            );

            string queryUrl = $"{openSkyBaseUrl}?{boundingBox.ToOpenSkyQuery()}";

            if (logToConsole)
            {
                Debug.Log($"OpenSky query URL -> {queryUrl}");
            }
        }
    }
}
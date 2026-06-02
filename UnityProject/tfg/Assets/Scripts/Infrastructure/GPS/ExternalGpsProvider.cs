using System;
using System.Collections;
using TFG.ARVisor.Domain.Models;
using TFG.ARVisor.Presentation.HUD;
using UnityEngine;
using UnityEngine.Networking;

namespace TFG.ARVisor.Infrastructure.Gps
{
    public class ExternalGpsProvider : MonoBehaviour
    {
        
        [Header("References")]
        [SerializeField] private HudController hudController;

        [Header("GPS Endpoint")]
        [SerializeField] private string gpsEndpointUrl = "http://192.168.1.100:5000/latest";

        [Header("Settings")]
        [SerializeField] private float refreshSeconds = 1f;
        [SerializeField] private int requestTimeoutSeconds = 2;
        [Header("Debug")]
        [SerializeField] private bool logGpsToConsole = false;

        public OwnshipGeoState CurrentState { get; private set; }

        private void Start()
        {
            StartCoroutine(PollGpsLoop());
        }

        private IEnumerator PollGpsLoop()
        {
            while (true)
            {
                yield return FetchLatestGps();
                yield return new WaitForSeconds(refreshSeconds);
            }
        }

        private IEnumerator FetchLatestGps()
        {
            if (string.IsNullOrWhiteSpace(gpsEndpointUrl))
            {
                UpdateHudWaiting();
                yield break;
            }

            using UnityWebRequest request = UnityWebRequest.Get(gpsEndpointUrl);
            request.timeout = requestTimeoutSeconds;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"GPS request failed: {request.error}");
                UpdateHudWaiting();
                yield break;
            }

            string json = request.downloadHandler.text;

            try
            {
                GpsPayload payload = JsonUtility.FromJson<GpsPayload>(json);

                AltitudeQuality altitudeQuality = GetAltitudeQuality(payload);

                double? altitudeMeters = payload.hasAltitude
                    ? payload.alt
                    : null;

                long timestamp = payload.timestamp > 0
                    ? payload.timestamp
                    : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                CurrentState = new OwnshipGeoState(
                    payload.lat,
                    payload.lon,
                    altitudeMeters,
                    timestamp,
                    altitudeQuality
                );

                UpdateHudWithGps();

                if (logGpsToConsole)
                {
                    Debug.Log(
                        $"GPS REAL -> Lat: {CurrentState.Latitude}, Lon: {CurrentState.Longitude}, " +
                        $"Alt: {CurrentState.AltitudeMeters}, Mode: {CurrentState.Mode}"
                    );
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Invalid GPS payload: {exception.Message}");
                UpdateHudWaiting();
            }
        }

        private AltitudeQuality GetAltitudeQuality(GpsPayload payload)
        {
            if (!payload.hasAltitude)
            {
                return AltitudeQuality.Missing;
            }

            return payload.altitudeReliable
                ? AltitudeQuality.Good
                : AltitudeQuality.Unreliable;
        }

        private void UpdateHudWithGps()
        {
            if (hudController == null || CurrentState == null)
            {
                return;
            }

            string modeText = CurrentState.Mode == OwnshipMode.Mode3D ? "3D" : "2D";
            string updateRateText = $"{1f / refreshSeconds:0.#} Hz";

            hudController.RenderSystemStatus(
                status: "ONLINE",
                gpsStatus: "REAL",
                mode: modeText,
                updateRate: updateRateText
            );

            hudController.RenderGpsCoordinates(
                CurrentState.Latitude,
                CurrentState.Longitude,
                CurrentState.AltitudeMeters
            );
        }

        private void UpdateHudWaiting()
        {
            if (hudController == null)
            {
                return;
            }

            hudController.RenderSystemStatus(
                status: "ONLINE",
                gpsStatus: "WAIT",
                mode: "--",
                updateRate: $"{1f / refreshSeconds:0.#} Hz"
            );
        }

        [Serializable]
        private class GpsPayload
        {
            public double lat;
            public double lon;
            public double alt;
            public bool hasAltitude;
            public bool altitudeReliable;
            public long timestamp;
        }
    }
}
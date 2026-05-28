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
 * 6. Filtra aeronaves por distancia real.
 * 7. Calcula el riesgo global del tráfico cercano.
 * 8. Guarda en caché los últimos datos válidos recibidos desde OpenSky.
 * 9. Recalcula cada segundo el TARGET del HUD usando la orientación actual del visor,
 *    sin volver a llamar a OpenSky.
 *
 * Importante:
 * La orientación del visor NO define el rumbo real del avión.
 * Solo se usa para saber qué zona está mirando el piloto y seleccionar
 * qué aeronave mostrar como TARGET en el HUD.
 *
 * Se conecta con:
 * - ExternalGpsProvider: obtiene la posición propia.
 * - BoundingBoxBuilder: genera la zona de búsqueda.
 * - OpenSkyParser: convierte el JSON de OpenSky en AircraftGeoState.
 * - TrafficFilter: filtra aeronaves por distancia real.
 * - RiskEngine: calcula el nivel de riesgo global.
 * - AircraftTargetSelector: selecciona el objetivo del HUD según la mirada.
 * - HeadsetOrientationProvider: obtiene la orientación relativa del visor.
 * - HudController: muestra la información en el visor.
 */

using System.Collections;
using System.Collections.Generic;
using TFG.ARVisor.Domain.Models;
using TFG.ARVisor.Domain.Services;
using TFG.ARVisor.Infrastructure.Gps;
using TFG.ARVisor.Infrastructure.XR;
using TFG.ARVisor.Presentation.HUD;
using UnityEngine;
using UnityEngine.Networking;

namespace TFG.ARVisor.Infrastructure.ApiClients
{
    public class OpenSkyApiClient : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ExternalGpsProvider gpsProvider;
        [SerializeField] private bool logAircraftListToConsole = false;
        [SerializeField] private HudController hudController;
        [SerializeField] private HeadsetOrientationProvider headsetOrientationProvider;
        [SerializeField] private OwnshipMotionEstimator motionEstimator;

        [Header("OpenSky Settings")]
        [SerializeField] private string openSkyBaseUrl = "https://opensky-network.org/api/states/all";
        [SerializeField] private double searchRadiusKm = 100.0;

        [Header("Target Selection")]
        [SerializeField] private bool prioritizeByHeadsetOrientation = true;
        [SerializeField] private double maxViewAngleDifferenceDegrees = 90.0;

        [Header("Request Settings")]
        [SerializeField] private float refreshSeconds = 300f;
        [SerializeField] private int requestTimeoutSeconds = 10;

        [Header("HUD Target Refresh")]
        [SerializeField] private float hudTargetRefreshSeconds = 1f;
        [SerializeField] private bool logHudTargetRefresh = false;

        [Header("Debug Settings")]
        [SerializeField] private bool logUrlToConsole = true;
        [SerializeField] private bool logRawJsonPreview = false;
        [SerializeField] private bool logConflictPredictionToConsole = true;
        [SerializeField] private float conflictLogIntervalSeconds = 3f;



        private Coroutine pollingCoroutine;

        private OwnshipGeoState lastOwnshipState;
        private List<AircraftGeoState> lastNearbyAircraft;
        private RiskAssessment lastRiskAssessment;
        private ConflictAssessment lastConflictAssessment;
        private float nextHudTargetRefreshTime;
        

        private float nextConflictLogTime;
        /// <summary>
        /// Inicia el ciclo de consultas a OpenSky cuando arranca la escena.
        /// </summary>
        private void Start()
        {
            pollingCoroutine = StartCoroutine(PollOpenSkyLoop());
        }

        /// <summary>
        /// Recalcula periódicamente qué aeronave debe mostrarse en el HUD usando
        /// los últimos datos recibidos desde OpenSky, sin realizar nuevas peticiones a la API.
        /// </summary>
        private void Update()
        {
            if (Time.time < nextHudTargetRefreshTime)
            {
                return;
            }

            nextHudTargetRefreshTime = Time.time + hudTargetRefreshSeconds;

            RefreshHudTargetFromCachedTraffic();
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

                if (request.responseCode == 429)
                {
                    Debug.LogWarning("OpenSky rate limit reached. Keeping last valid HUD data.");
                }

                yield break;
            }

            string json = request.downloadHandler.text;

            ProcessOpenSkyResponse(json, ownshipState);
        }

        /// <summary>
        /// Procesa el JSON recibido desde OpenSky, filtra aeronaves, calcula riesgo
        /// y guarda los datos en caché para que el HUD pueda recalcular el TARGET sin llamar otra vez a la API.
        /// </summary>
        private void ProcessOpenSkyResponse(string json, OwnshipGeoState ownshipState)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("OpenSky response is empty.");

                RiskAssessment emptyAssessment = new RiskAssessment(
                    RiskLevel.Low,
                    "NO ALERTS",
                    null,
                    null,
                    null,
                    0
                );

                lastOwnshipState = ownshipState;
                lastNearbyAircraft = new List<AircraftGeoState>();
                lastRiskAssessment = emptyAssessment;

                RefreshHudTargetFromCachedTraffic();
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

           RiskAssessment riskAssessment = RiskEngine.Evaluate(
                ownshipState,
                nearbyAircraft
            );

            ConflictAssessment conflictAssessment = EvaluateConflictPrediction(
                ownshipState,
                nearbyAircraft
            );

            lastOwnshipState = ownshipState;
            lastNearbyAircraft = nearbyAircraft;
            lastRiskAssessment = riskAssessment;
            lastConflictAssessment = conflictAssessment;

            RefreshHudTargetFromCachedTraffic();

            LogAircraftList(ownshipState, nearbyAircraft);

            if (logRawJsonPreview)
            {
                int previewLength = Mathf.Min(json.Length, 600);
                Debug.Log($"OpenSky JSON preview -> {json.Substring(0, previewLength)}");
            }
        }

        /// <summary>
        /// Actualiza el HUD usando los últimos datos válidos de tráfico, la orientación actual del visor
        /// y la predicción actualizada de conflicto, sin realizar nuevas peticiones a OpenSky.
        /// </summary>
        private void RefreshHudTargetFromCachedTraffic()
        {
            if (lastOwnshipState == null || lastRiskAssessment == null)
            {
                return;
            }

            AircraftTargetSelectionResult targetSelection = SelectHudTarget(
                lastOwnshipState,
                lastNearbyAircraft
            );

            lastConflictAssessment = EvaluateConflictPrediction(
                lastOwnshipState,
                lastNearbyAircraft
            );

            UpdateHudWithRisk(lastRiskAssessment, targetSelection, lastConflictAssessment);

            if (logHudTargetRefresh)
            {
                Debug.Log("HUD target and conflict prediction refreshed from cached traffic.");
            }
        }

        /// <summary>
        /// Selecciona la aeronave que debe mostrarse como TARGET en el HUD.
        /// Si está disponible la orientación del visor, se usa la dirección de mirada.
        /// Si no está disponible, se mantiene el comportamiento anterior.
        /// </summary>
        private AircraftTargetSelectionResult SelectHudTarget(
            OwnshipGeoState ownshipState,
            List<AircraftGeoState> nearbyAircraft)
        {
            if (!prioritizeByHeadsetOrientation ||
                headsetOrientationProvider == null ||
                nearbyAircraft == null ||
                nearbyAircraft.Count == 0)
            {
                return AircraftTargetSelector.Empty();
            }

            return AircraftTargetSelector.SelectByHeadsetDirection(
                ownshipState,
                nearbyAircraft,
                headsetOrientationProvider.GetAircraftHeadingDegrees(),
                headsetOrientationProvider.GetHeadRelativeYawDegrees(),
                maxViewAngleDifferenceDegrees
            );
        }

        /// <summary>
        /// Actualiza el HUD combinando riesgo básico, selección visual de target
        /// y predicción inicial de conflicto de trayectoria.
        /// </summary>
        private void UpdateHudWithRisk(
            RiskAssessment riskAssessment,
            AircraftTargetSelectionResult targetSelection,
            ConflictAssessment conflictAssessment)
        {
            if (hudController == null || riskAssessment == null)
            {
                return;
            }

            bool conflictHasPriority = ShouldPrioritizeConflict(conflictAssessment);

            AircraftGeoState relevantAircraft = null;

            if (conflictHasPriority)
            {
                relevantAircraft = conflictAssessment.Aircraft;
            }
            else if (targetSelection != null && targetSelection.HasTarget)
            {
                relevantAircraft = targetSelection.SelectedAircraft;
            }
            else
            {
                relevantAircraft = riskAssessment.MostRelevantAircraft;
            }

            double? targetDistanceKm = GetDisplayDistanceKm(
                riskAssessment,
                targetSelection,
                conflictAssessment,
                conflictHasPriority
            );

            string nearestDistanceText = targetDistanceKm.HasValue
                ? $"{targetDistanceKm.Value:0.0} KM"
                : "--";

            RiskLevel displayRiskLevel = GetOverallRiskLevel(
                riskAssessment,
                conflictAssessment
            );

            string displayAlertMessage = GetOverallAlertMessage(
                riskAssessment,
                conflictAssessment,
                displayRiskLevel
            );

            string callsign = GetAircraftCallsign(relevantAircraft);
            string country = relevantAircraft != null ? relevantAircraft.OriginCountry : "";
            string altitude = FormatAltitude(relevantAircraft);
            string heading = FormatHeading(relevantAircraft);

            TrafficSnapshot snapshot = new TrafficSnapshot(
                nearbyAircraft: riskAssessment.AircraftCount,
                nearestDistance: nearestDistanceText,
                riskLevel: displayRiskLevel,
                alertMessage: displayAlertMessage,
                relevantCallsign: callsign,
                relevantCountry: country,
                relevantAltitude: altitude,
                relevantHeading: heading,
                viewSector: BuildViewSectorText(),
                targetSector: BuildTargetSectorTextForAircraft(relevantAircraft),
                selectionMode: conflictHasPriority
                    ? "CONFLICT"
                    : GetSelectionModeText(targetSelection),
                conflictStatus: GetConflictStatusText(conflictAssessment),
                closestApproachDistance: FormatClosestApproach(conflictAssessment),
                timeToClosestApproach: FormatTimeToClosestApproach(conflictAssessment),
                motionStatus: GetMotionStatusText(conflictAssessment)
            );

            hudController.RenderTraffic(snapshot);

            LogTargetSelection(riskAssessment, targetSelection, callsign, nearestDistanceText);
            LogConflictAssessment(conflictAssessment);
        }

        /// <summary>
        /// Muestra en consola las aeronaves detectadas con sus datos principales y distancia.
        /// </summary>
        private void LogAircraftList(OwnshipGeoState ownshipState, List<AircraftGeoState> aircraft)
        {
            if (!logAircraftListToConsole)
            {
                return;
            }

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
        /// Muestra en consola cómo se ha seleccionado la aeronave objetivo del HUD.
        /// </summary>
        private void LogTargetSelection(
            RiskAssessment riskAssessment,
            AircraftTargetSelectionResult targetSelection,
            string callsign,
            string distanceText)
        {

            if (!logHudTargetRefresh)
            {
                return;
            }
            if (riskAssessment == null)
            {
                return;
            }

            if (targetSelection == null || !targetSelection.HasTarget)
            {
                Debug.Log(
                    $"Risk assessment -> " +
                    $"Aircraft: {riskAssessment.AircraftCount}, " +
                    $"Relevant: {callsign}, " +
                    $"Nearest: {distanceText}, " +
                    $"Risk: {riskAssessment.RiskLevel}, " +
                    $"Alert: {riskAssessment.AlertMessage}"
                );

                return;
            }

            string selectionMode = targetSelection.SelectedByViewDirection
                ? "HEADSET_VIEW"
                : "CLOSEST_FALLBACK";

            Debug.Log(
                $"Target selection -> " +
                $"Mode: {selectionMode}, " +
                $"Target: {callsign}, " +
                $"Distance: {distanceText}, " +
                $"Bearing: {targetSelection.BearingDegrees:0.0}°, " +
                $"RelativeBearing: {targetSelection.RelativeBearingDegrees:0.0}°, " +
                $"HeadYaw: {targetSelection.HeadRelativeYawDegrees:0.0}°, " +
                $"AngleDiff: {targetSelection.ViewAngleDifferenceDegrees:0.0}°, " +
                $"ViewSector: {GetViewSectorText(targetSelection)}, " +
                $"TargetSector: {GetTargetSectorText(targetSelection)}, " +
                $"Risk: {riskAssessment.RiskLevel}"
            );
        }

        /// <summary>
        /// Devuelve el callsign de la aeronave relevante.
        /// Si no existe callsign, usa el identificador de la aeronave.
        /// Si tampoco existe identificador, muestra UNKNOWN.
        /// </summary>
        private string GetAircraftCallsign(AircraftGeoState aircraft)
        {
            if (aircraft == null)
            {
                return "";
            }

            if (!string.IsNullOrWhiteSpace(aircraft.Callsign))
            {
                return aircraft.Callsign.Trim();
            }

            if (!string.IsNullOrWhiteSpace(aircraft.Id))
            {
                return aircraft.Id.Trim().ToUpper();
            }

            return "UNKNOWN";
        }

        /// <summary>
        /// Formatea la altitud de la aeronave para mostrarla en el HUD.
        /// Si la altitud no está disponible, muestra "--".
        /// </summary>
        private string FormatAltitude(AircraftGeoState aircraft)
        {
            if (aircraft == null || !aircraft.AltitudeMeters.HasValue)
            {
                return "--";
            }

            return $"{aircraft.AltitudeMeters.Value:0} M";
        }

        /// <summary>
        /// Formatea el rumbo de la aeronave para mostrarlo en el HUD.
        /// Si el rumbo no está disponible, muestra "--".
        /// </summary>
        private string FormatHeading(AircraftGeoState aircraft)
        {
            if (aircraft == null || !aircraft.HeadingDegrees.HasValue)
            {
                return "--";
            }

            return $"{aircraft.HeadingDegrees.Value:0}°";
        }

        /// <summary>
        /// Devuelve el texto del sector hacia el que está mirando el piloto.
        /// </summary>
        private string GetViewSectorText(AircraftTargetSelectionResult targetSelection)
        {
            if (targetSelection == null || !targetSelection.HeadRelativeYawDegrees.HasValue)
            {
                return "VIEW --";
            }

            return $"VIEW {GetSectorName(targetSelection.HeadRelativeYawDegrees.Value)}";
        }

        /// <summary>
        /// Devuelve el texto del sector donde se encuentra la aeronave seleccionada respecto al avión.
        /// </summary>
        private string GetTargetSectorText(AircraftTargetSelectionResult targetSelection)
        {
            if (targetSelection == null || !targetSelection.RelativeBearingDegrees.HasValue)
            {
                return "SECTOR --";
            }

            return $"SECTOR {GetSectorName(targetSelection.RelativeBearingDegrees.Value)}";
        }

        /// <summary>
        /// Devuelve el modo usado para seleccionar el objetivo del HUD.
        /// </summary>
        private string GetSelectionModeText(AircraftTargetSelectionResult targetSelection)
        {
            if (targetSelection == null || !targetSelection.HasTarget)
            {
                return "NO LOCK";
            }

            return targetSelection.SelectedByViewDirection ? "VIEW LOCK" : "NEAREST";
        }


        /// <summary>
/// Ejecuta la predicción de conflicto usando el movimiento propio estimado
/// y las aeronaves cercanas recibidas desde OpenSky.
/// </summary>
private ConflictAssessment EvaluateConflictPrediction(
    OwnshipGeoState ownshipState,
    List<AircraftGeoState> nearbyAircraft)
{
    if (nearbyAircraft == null || nearbyAircraft.Count == 0)
    {
        return null;
    }

    OwnshipMotionState motionState = motionEstimator != null
        ? motionEstimator.CurrentMotionState
        : null;

    ConflictAssessment assessment = ConflictPredictionEngine.EvaluateMostCritical(
        ownshipState,
        motionState,
        nearbyAircraft
    );

    return assessment;
}

/// <summary>
/// Indica si la predicción de conflicto debe imponerse sobre el target visual por mirada.
/// </summary>
private bool ShouldPrioritizeConflict(ConflictAssessment conflictAssessment)
{
    return conflictAssessment != null &&
           conflictAssessment.HasPrediction &&
           conflictAssessment.RiskLevel != RiskLevel.Low;
}

/// <summary>
/// Devuelve la distancia que debe mostrarse en el HUD.
/// Si hay conflicto prioritario, muestra la distancia actual de la aeronave conflictiva.
/// </summary>
private double? GetDisplayDistanceKm(
    RiskAssessment riskAssessment,
    AircraftTargetSelectionResult targetSelection,
    ConflictAssessment conflictAssessment,
    bool conflictHasPriority)
{
    if (conflictHasPriority && conflictAssessment != null)
    {
        return conflictAssessment.CurrentDistanceKm;
    }

    if (targetSelection != null && targetSelection.SelectedDistanceKm.HasValue)
    {
        return targetSelection.SelectedDistanceKm.Value;
    }

    return riskAssessment.NearestDistanceKm;
}

/// <summary>
/// Combina el riesgo básico por cercanía y el riesgo calculado por predicción.
/// </summary>
private RiskLevel GetOverallRiskLevel(
    RiskAssessment riskAssessment,
    ConflictAssessment conflictAssessment)
{
    RiskLevel basicRisk = riskAssessment != null
        ? riskAssessment.RiskLevel
        : RiskLevel.Low;

    RiskLevel conflictRisk = conflictAssessment != null
        ? conflictAssessment.RiskLevel
        : RiskLevel.Low;

    return GetRiskPriority(conflictRisk) > GetRiskPriority(basicRisk)
        ? conflictRisk
        : basicRisk;
}

/// <summary>
/// Devuelve el mensaje de alerta global que debe aparecer en la parte superior del HUD.
/// </summary>
private string GetOverallAlertMessage(
    RiskAssessment riskAssessment,
    ConflictAssessment conflictAssessment,
    RiskLevel overallRisk)
{
    if (conflictAssessment != null &&
        conflictAssessment.HasPrediction &&
        conflictAssessment.RiskLevel == overallRisk &&
        overallRisk != RiskLevel.Low)
    {
        return conflictAssessment.AlertMessage;
    }

    return riskAssessment != null
        ? riskAssessment.AlertMessage
        : "NO ALERTS";
}

/// <summary>
/// Devuelve una prioridad numérica para comparar riesgos.
/// </summary>
private int GetRiskPriority(RiskLevel riskLevel)
{
    switch (riskLevel)
    {
        case RiskLevel.High:
            return 3;

        case RiskLevel.Medium:
            return 2;

        default:
            return 1;
    }
}

/// <summary>
/// Devuelve el texto del sector hacia el que está mirando el piloto.
/// </summary>
private string BuildViewSectorText()
{
    if (headsetOrientationProvider == null)
    {
        return "VIEW --";
    }

    double headYaw = headsetOrientationProvider.GetHeadRelativeYawDegrees();

    return $"VIEW {GetSectorName(headYaw)}";
}

/// <summary>
/// Devuelve el sector relativo donde se encuentra una aeronave respecto al frente del avión.
/// </summary>
private string BuildTargetSectorTextForAircraft(AircraftGeoState aircraft)
{
    if (aircraft == null || lastOwnshipState == null)
    {
        return "SECTOR --";
    }

    double aircraftHeadingDegrees = headsetOrientationProvider != null
        ? headsetOrientationProvider.GetAircraftHeadingDegrees()
        : 0.0;

    double bearingDegrees = GeoBearingCalculator.BearingDegrees(
        lastOwnshipState,
        aircraft
    );

    double relativeBearingDegrees = GeoBearingCalculator.NormalizeSigned180(
        bearingDegrees - aircraftHeadingDegrees
    );

    return $"SECTOR {GetSectorName(relativeBearingDegrees)}";
}

/// <summary>
/// Convierte un ángulo relativo en un sector simple del avión.
/// </summary>
private string GetSectorName(double relativeAngleDegrees)
{
    double angle = GeoBearingCalculator.NormalizeSigned180(relativeAngleDegrees);

    if (angle >= -45.0 && angle <= 45.0)
    {
        return "FRONT";
    }

    if (angle > 45.0 && angle < 135.0)
    {
        return "RIGHT";
    }

    if (angle < -45.0 && angle > -135.0)
    {
        return "LEFT";
    }

    return "REAR";
}

/// <summary>
/// Devuelve un texto resumido sobre el estado de predicción de conflicto.
/// </summary>
private string GetConflictStatusText(ConflictAssessment conflictAssessment)
{
    if (conflictAssessment == null)
    {
        return "PRED WAIT";
    }

    if (!conflictAssessment.HasPrediction)
    {
        return "PRED WAIT";
    }

    switch (conflictAssessment.RiskLevel)
    {
        case RiskLevel.High:
            return "CONFLICT RISK";

        case RiskLevel.Medium:
            return "TRAJECTORY WATCH";

        default:
            return "PATH CLEAR";
    }
}

/// <summary>
/// Formatea la distancia CPA para mostrarla en el HUD.
/// </summary>
private string FormatClosestApproach(ConflictAssessment conflictAssessment)
{
    if (conflictAssessment == null ||
        !conflictAssessment.HasPrediction ||
        !conflictAssessment.ClosestApproachDistanceKm.HasValue)
    {
        return "--";
    }

    return $"{conflictAssessment.ClosestApproachDistanceKm.Value:0.0} KM";
}

/// <summary>
/// Formatea el tiempo TCPA para mostrarlo en el HUD.
/// </summary>
private string FormatTimeToClosestApproach(ConflictAssessment conflictAssessment)
{
    if (conflictAssessment == null ||
        !conflictAssessment.HasPrediction ||
        !conflictAssessment.TimeToClosestApproachSeconds.HasValue)
    {
        return "--";
    }

    int totalSeconds = (int)System.Math.Round(
        conflictAssessment.TimeToClosestApproachSeconds.Value
    );

    int minutes = totalSeconds / 60;
    int seconds = totalSeconds % 60;

    return $"{minutes:00}:{seconds:00}";
}

/// <summary>
/// Devuelve el estado de movimiento propio usado por la predicción.
/// </summary>
private string GetMotionStatusText(ConflictAssessment conflictAssessment)
{
    if (conflictAssessment == null)
    {
        return "MOTION --";
    }

    if (!conflictAssessment.HasPrediction)
    {
        return "MOTION WAIT";
    }

    return "MOTION OK";
}

/// <summary>
/// Muestra una única línea resumida de predicción de conflicto cada cierto tiempo.
/// Sirve para comprobar si el cambio de rumbo simulado afecta al CPA/TCPA.
/// </summary>
private void LogConflictAssessment(ConflictAssessment conflictAssessment)
{
    if (!logConflictPredictionToConsole || conflictAssessment == null)
    {
        return;
    }

    if (Time.time < nextConflictLogTime)
    {
        return;
    }

    nextConflictLogTime = Time.time + conflictLogIntervalSeconds;

    OwnshipMotionState motionState = motionEstimator != null
        ? motionEstimator.CurrentMotionState
        : null;

    string ownshipText = motionState != null && motionState.HasReliableMotion
        ? $"Ownship: {motionState.TrackDegrees:0.0}° / {motionState.SpeedMps:0.0} m/s"
        : "Ownship: NO MOTION";

    Debug.Log(
        $"CONFLICT TEST -> " +
        $"{ownshipText} | " +
        $"Aircraft: {GetAircraftCallsign(conflictAssessment.Aircraft)} | " +
        $"Current: {conflictAssessment.CurrentDistanceKm:0.0} KM | " +
        $"CPA: {FormatClosestApproach(conflictAssessment)} | " +
        $"TCPA: {FormatTimeToClosestApproach(conflictAssessment)} | " +
        $"Risk: {conflictAssessment.RiskLevel} | " +
        $"Reason: {conflictAssessment.Reason}"
    );
}
    }

    
}

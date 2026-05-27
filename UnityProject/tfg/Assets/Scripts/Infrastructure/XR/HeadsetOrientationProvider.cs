/*
 * HeadsetOrientationProvider.cs
 * ------------------------------------------------------------
 * Este script obtiene la orientación actual del visor dentro de Unity.
 *
 * Importante:
 * La orientación del visor NO se usa como rumbo real del avión.
 * El rumbo del avión se mantiene como una referencia separada.
 *
 * Funcionamiento general:
 * 1. Al iniciar la escena guarda la orientación inicial de la cabeza.
 * 2. Esa orientación inicial se considera la mirada al frente de la cabina.
 * 3. A partir de ahí calcula cuánto gira la cabeza el piloto a izquierda/derecha.
 * 4. Ese giro se usa para seleccionar qué aeronave mostrar en el HUD.
 *
 * Se conecta con:
 * - Main Camera / XR Camera: obtiene la orientación del visor.
 * - OpenSkyApiClient: consulta la orientación relativa de la cabeza.
 * - AircraftTargetSelector: compara la mirada con la posición relativa de aeronaves.
 */

using TFG.ARVisor.Domain.Services;
using UnityEngine;

namespace TFG.ARVisor.Infrastructure.XR
{
    public class HeadsetOrientationProvider : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform headsetTransform;

        [Header("Aircraft Reference")]
        [SerializeField] private double aircraftHeadingDegrees = 0.0;

        [Header("Calibration")]
        [SerializeField] private bool calibrateOnStart = true;
        [SerializeField] private bool logOrientationToConsole = false;

        private float calibratedForwardYawDegrees;

        /// <summary>
        /// Inicializa la referencia de orientación del visor.
        /// </summary>
        private void Start()
        {
            ResolveHeadsetTransform();

            if (calibrateOnStart)
            {
                CalibrateForwardDirection();
            }
        }

        /// <summary>
        /// Muestra en consola la orientación relativa de la cabeza si está activada la depuración.
        /// </summary>
        private void Update()
        {
            if (logOrientationToConsole)
            {
                Debug.Log(
                    $"Headset orientation -> Relative yaw: {GetHeadRelativeYawDegrees():0.0}°, " +
                    $"Aircraft heading: {GetAircraftHeadingDegrees():0.0}°"
                );
            }
        }

        /// <summary>
        /// Guarda la orientación actual del visor como frente inicial de la cabina.
        /// </summary>
        public void CalibrateForwardDirection()
        {
            ResolveHeadsetTransform();

            if (headsetTransform == null)
            {
                Debug.LogWarning("HeadsetOrientationProvider: headset transform is missing.");
                return;
            }

            calibratedForwardYawDegrees = headsetTransform.eulerAngles.y;

            Debug.Log($"Headset calibrated. Forward yaw: {calibratedForwardYawDegrees:0.0}°");
        }

        /// <summary>
        /// Devuelve cuánto ha girado la cabeza respecto al frente inicial de la cabina.
        /// Valores negativos indican izquierda y valores positivos indican derecha.
        /// </summary>
        public double GetHeadRelativeYawDegrees()
        {
            ResolveHeadsetTransform();

            if (headsetTransform == null)
            {
                return 0.0;
            }

            double currentYaw = headsetTransform.eulerAngles.y;

            return GeoBearingCalculator.NormalizeSigned180(
                currentYaw - calibratedForwardYawDegrees
            );
        }

        /// <summary>
        /// Devuelve el rumbo de referencia del avión.
        /// En el prototipo se puede configurar manualmente desde el Inspector.
        /// </summary>
        public double GetAircraftHeadingDegrees()
        {
            return aircraftHeadingDegrees;
        }

        /// <summary>
        /// Intenta localizar automáticamente la cámara principal si no se ha asignado una referencia.
        /// </summary>
        private void ResolveHeadsetTransform()
        {
            if (headsetTransform != null)
            {
                return;
            }

            Camera mainCamera = Camera.main;

            if (mainCamera != null)
            {
                headsetTransform = mainCamera.transform;
            }
        }
    }
}
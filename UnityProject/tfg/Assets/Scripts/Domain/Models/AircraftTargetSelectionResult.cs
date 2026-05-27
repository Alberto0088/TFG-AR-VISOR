/*
 * AircraftTargetSelectionResult.cs
 * ------------------------------------------------------------
 * Este modelo representa el resultado de seleccionar una aeronave objetivo
 * para mostrar en el HUD.
 *
 * No sustituye al cálculo de riesgo global. Su función es indicar qué
 * aeronave debe mostrarse como TARGET en el panel derecho, teniendo en cuenta
 * la dirección hacia la que mira el piloto.
 *
 * Se conecta con:
 * - AircraftTargetSelector: genera este resultado.
 * - OpenSkyApiClient: usa este resultado para actualizar el HUD.
 * - HudController: muestra la aeronave seleccionada como TARGET.
 */

namespace TFG.ARVisor.Domain.Models
{
    public class AircraftTargetSelectionResult
    {
        public AircraftGeoState SelectedAircraft { get; }
        public double? SelectedDistanceKm { get; }
        public double? BearingDegrees { get; }
        public double? RelativeBearingDegrees { get; }
        public double? HeadRelativeYawDegrees { get; }
        public double? ViewAngleDifferenceDegrees { get; }
        public bool SelectedByViewDirection { get; }

        public bool HasTarget => SelectedAircraft != null;

        /// <summary>
        /// Crea el resultado de selección de la aeronave que se mostrará como objetivo en el HUD.
        /// </summary>
        public AircraftTargetSelectionResult(
            AircraftGeoState selectedAircraft,
            double? selectedDistanceKm,
            double? bearingDegrees,
            double? relativeBearingDegrees,
            double? headRelativeYawDegrees,
            double? viewAngleDifferenceDegrees,
            bool selectedByViewDirection)
        {
            SelectedAircraft = selectedAircraft;
            SelectedDistanceKm = selectedDistanceKm;
            BearingDegrees = bearingDegrees;
            RelativeBearingDegrees = relativeBearingDegrees;
            HeadRelativeYawDegrees = headRelativeYawDegrees;
            ViewAngleDifferenceDegrees = viewAngleDifferenceDegrees;
            SelectedByViewDirection = selectedByViewDirection;
        }
    }
}
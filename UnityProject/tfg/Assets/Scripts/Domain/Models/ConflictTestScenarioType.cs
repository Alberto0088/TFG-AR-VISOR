/*
 * ConflictTestScenarioType.cs
 * ------------------------------------------------------------
 * Enumera los escenarios controlados de conflicto disponibles para pruebas.
 *
 * Estos escenarios permiten validar el motor CPA/TCPA sin depender de que
 * OpenSky proporcione en ese momento una aeronave real en trayectoria peligrosa.
 *
 * Se conecta con:
 * - ConflictTestScenarioFactory: genera aeronaves simuladas.
 * - OpenSkyApiClient: activa o desactiva estos escenarios desde el Inspector.
 */

namespace TFG.ARVisor.Domain.Models
{
    public enum ConflictTestScenarioType
    {
        SafeParallel,
        CrossingTraffic,
        HeadOnConflict
    }
}
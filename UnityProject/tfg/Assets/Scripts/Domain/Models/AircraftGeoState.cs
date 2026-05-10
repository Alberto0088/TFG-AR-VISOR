/*
 * AircraftGeoState.cs
 * ------------------------------------------------------------
 * Este modelo representa una aeronave detectada en coordenadas geográficas.
 *
 * Su función es almacenar los datos ya normalizados de una aeronave después
 * de haber sido recibida desde una fuente externa, como OpenSky.
 *
 * Este modelo NO depende directamente del formato interno de OpenSky.
 * Es decir, el resto del sistema no trabajará con arrays como state[0],
 * state[5] o state[6], sino con propiedades claras como Latitude,
 * Longitude o AltitudeMeters.
 *
 * Se conecta con:
 * - OpenSkyParser: convertirá el JSON de OpenSky en AircraftGeoState.
 * - TrafficFilter: usará estos datos para filtrar aeronaves cercanas.
 * - RiskEngine: usará estos datos para calcular riesgo.
 * - HudController: recibirá información procesada a partir de estas aeronaves.
 */

namespace TFG.ARVisor.Domain.Models
{
    public class AircraftGeoState
    {
        public string Id { get; }
        public string Callsign { get; }
        public string OriginCountry { get; }
        public double Latitude { get; }
        public double Longitude { get; }
        public double? AltitudeMeters { get; }
        public double? VelocityMps { get; }
        public double? HeadingDegrees { get; }
        public double? VerticalRateMps { get; }
        public long LastContactUtc { get; }

        /// <summary>
        /// Crea una aeronave normalizada a partir de los datos ya interpretados de una fuente externa.
        /// </summary>
        public AircraftGeoState(
            string id,
            string callsign,
            string originCountry,
            double latitude,
            double longitude,
            double? altitudeMeters,
            double? velocityMps,
            double? headingDegrees,
            double? verticalRateMps,
            long lastContactUtc)
        {
            Id = id;
            Callsign = callsign;
            OriginCountry = originCountry;
            Latitude = latitude;
            Longitude = longitude;
            AltitudeMeters = altitudeMeters;
            VelocityMps = velocityMps;
            HeadingDegrees = headingDegrees;
            VerticalRateMps = verticalRateMps;
            LastContactUtc = lastContactUtc;
        }
    }
}
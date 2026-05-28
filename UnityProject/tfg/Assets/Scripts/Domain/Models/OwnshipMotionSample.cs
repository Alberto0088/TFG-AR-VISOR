/*
 * OwnshipMotionSample.cs
 * ------------------------------------------------------------
 * Este modelo representa una muestra histórica de posición propia.
 *
 * Su función es guardar una posición GPS concreta junto con el tiempo en el
 * que fue recibida. Estas muestras se usan después para estimar el rumbo y
 * la velocidad propios a partir del movimiento reciente.
 *
 * Se conecta con:
 * - OwnshipMotionEstimator: almacena y analiza estas muestras.
 * - OwnshipGeoState: proporciona latitud, longitud y altitud.
 */

using System;

namespace TFG.ARVisor.Domain.Models
{
    public class OwnshipMotionSample
    {
        public double Latitude { get; }
        public double Longitude { get; }
        public double? AltitudeMeters { get; }
        public DateTime TimestampUtc { get; }

        /// <summary>
        /// Crea una muestra histórica de posición propia.
        /// </summary>
        public OwnshipMotionSample(
            double latitude,
            double longitude,
            double? altitudeMeters,
            DateTime timestampUtc)
        {
            Latitude = latitude;
            Longitude = longitude;
            AltitudeMeters = altitudeMeters;
            TimestampUtc = timestampUtc;
        }
    }
}
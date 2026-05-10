/*
 * GeoBoundingBox.cs
 * ------------------------------------------------------------
 * Este modelo representa una caja geográfica delimitada por una
 * latitud mínima, longitud mínima, latitud máxima y longitud máxima.
 *
 * Se utiliza para consultar APIs externas como OpenSky, evitando
 * pedir tráfico aéreo mundial y limitando la búsqueda a una zona
 * cercana a la posición propia del usuario/avión.
 *
 * Importante:
 * OpenSky espera los números decimales con punto, no con coma.
 * Por eso se usa CultureInfo.InvariantCulture al generar la query.
 */

using System.Globalization;

namespace TFG.ARVisor.Domain.Models
{
    public class GeoBoundingBox
    {
        public double MinLatitude { get; }
        public double MinLongitude { get; }
        public double MaxLatitude { get; }
        public double MaxLongitude { get; }

        /// <summary>
        /// Crea una caja geográfica con los límites necesarios para consultar tráfico cercano.
        /// </summary>
        public GeoBoundingBox(
            double minLatitude,
            double minLongitude,
            double maxLatitude,
            double maxLongitude)
        {
            MinLatitude = minLatitude;
            MinLongitude = minLongitude;
            MaxLatitude = maxLatitude;
            MaxLongitude = maxLongitude;
        }

        /// <summary>
        /// Devuelve la bounding box con el formato de parámetros que necesita OpenSky.
        /// Se usa InvariantCulture para asegurar decimales con punto.
        /// </summary>
        public string ToOpenSkyQuery()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "lamin={0}&lomin={1}&lamax={2}&lomax={3}",
                MinLatitude,
                MinLongitude,
                MaxLatitude,
                MaxLongitude
            );
        }
    }
}
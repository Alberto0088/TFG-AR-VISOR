/*
 * OpenSkyParser.cs
 * ------------------------------------------------------------
 * Este script convierte la respuesta JSON de OpenSky en una lista
 * de aeronaves propias del sistema.
 *
 * OpenSky devuelve cada aeronave como un array de valores, por ejemplo:
 * state[0] = icao24
 * state[1] = callsign
 * state[5] = longitude
 * state[6] = latitude
 * state[7] = baro_altitude
 * state[13] = geo_altitude
 *
 * Este parser evita que el resto del proyecto tenga que trabajar con
 * esos índices directamente. A partir de aquí, el sistema usará modelos
 * internos claros como AircraftGeoState.
 *
 * Se conecta con:
 * - OpenSkyApiClient: recibe el JSON crudo desde la API.
 * - AircraftGeoState: modelo interno normalizado de aeronave.
 * - TrafficFilter / RiskEngine: futuros módulos que usarán estas aeronaves.
 */

using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TFG.ARVisor.Domain.Models;

namespace TFG.ARVisor.Infrastructure.ApiClients
{
    public static class OpenSkyParser
    {
        /// <summary>
        /// Convierte el JSON completo de OpenSky en una lista de aeronaves normalizadas.
        /// </summary>
        public static List<AircraftGeoState> ParseAircraft(string json)
        {
            List<AircraftGeoState> aircraftList = new List<AircraftGeoState>();

            if (string.IsNullOrWhiteSpace(json))
            {
                return aircraftList;
            }

            JObject root = JObject.Parse(json);
            JArray states = root["states"] as JArray;

            if (states == null)
            {
                return aircraftList;
            }

            foreach (JToken stateToken in states)
            {
                if (stateToken is not JArray state)
                {
                    continue;
                }

                AircraftGeoState aircraft = ParseStateVector(state);

                if (aircraft != null)
                {
                    aircraftList.Add(aircraft);
                }
            }

            return aircraftList;
        }

        /// <summary>
        /// Convierte un único vector de estado de OpenSky en un AircraftGeoState.
        /// </summary>
        private static AircraftGeoState ParseStateVector(JArray state)
        {
            double? longitude = GetNullableDouble(state, 5);
            double? latitude = GetNullableDouble(state, 6);

            if (!latitude.HasValue || !longitude.HasValue)
            {
                return null;
            }

            string id = GetString(state, 0);
            string callsign = GetString(state, 1).Trim();
            string originCountry = GetString(state, 2);

            long lastContactUtc = GetLong(state, 4);

            double? baroAltitude = GetNullableDouble(state, 7);
            double? geoAltitude = GetNullableDouble(state, 13);
            double? altitudeMeters = geoAltitude ?? baroAltitude;

            double? velocityMps = GetNullableDouble(state, 9);
            double? headingDegrees = GetNullableDouble(state, 10);
            double? verticalRateMps = GetNullableDouble(state, 11);

            return new AircraftGeoState(
                id,
                callsign,
                originCountry,
                latitude.Value,
                longitude.Value,
                altitudeMeters,
                velocityMps,
                headingDegrees,
                verticalRateMps,
                lastContactUtc
            );
        }

        /// <summary>
        /// Obtiene un texto de una posición del array de OpenSky.
        /// </summary>
        private static string GetString(JArray state, int index)
        {
            if (index >= state.Count || state[index].Type == JTokenType.Null)
            {
                return string.Empty;
            }

            return state[index].ToString();
        }

        /// <summary>
        /// Obtiene un número decimal de una posición del array de OpenSky.
        /// Si el dato no existe o viene como null, devuelve null.
        /// </summary>
        private static double? GetNullableDouble(JArray state, int index)
        {
            if (index >= state.Count || state[index].Type == JTokenType.Null)
            {
                return null;
            }

            return state[index].Value<double>();
        }

        /// <summary>
        /// Obtiene un número entero largo de una posición del array de OpenSky.
        /// Si el dato no existe, devuelve 0.
        /// </summary>
        private static long GetLong(JArray state, int index)
        {
            if (index >= state.Count || state[index].Type == JTokenType.Null)
            {
                return 0;
            }

            return state[index].Value<long>();
        }
    }
}
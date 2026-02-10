# Modelo de datos interno (v1)

## Objetivo

Definir un modelo de datos común para el prototipo, desacoplando las fuentes externas (APIs y GPS) de la lógica de procesamiento y de la visualización en Unity. 
Para ello se separan datos geográficos (lat/lon/alt) de datos locales (x/y/z en metros) utilizados por el motor gráfico (Unity).

---

## Tipos básicos

### GeoPoint
- lat: double
- lon: double

---

## Posición propia (GPS smartphone)

### OwnshipGeoState
- lat: double
- lon: double
- alt_m: double (opcional)
- timestampUtc: long

### AltitudeQuality
- GOOD
- UNRELIABLE
- MISSING

### OwnshipMode
- MODE_3D
- MODE_2D

### OwnshipStatus
- hasValidFix: bool
- altitudeQuality: AltitudeQuality
- mode: OwnshipMode

---

## Tráfico aéreo (API)

### AircraftGeoState
- id: string
- callsign: string (opcional)
- lat: double
- lon: double
- alt_m: double (opcional)
- heading_deg: double (opcional)
- speed_mps: double (opcional)
- timestampUtc: long

---

## Tráfico en coordenadas locales (Unity)

### AircraftLocalState
- id: string
- relativePosition_m: Vector3
- horizontalDistance_m: double
- verticalSeparation_m: double (opcional)
- lastUpdateUtc: long

---

## Evaluación de riesgo

### RiskLevel
- LOW
- MEDIUM
- HIGH

### RiskAssessment
- targetId: string
- riskLevel: RiskLevel
- horizontalDistance_m: double
- verticalSeparation_m: double (opcional)
- reason: string

---

## Meteorología (resumen)

### WeatherState
- locationName: string
- windDir_deg: double (opcional)
- windSpeed_kt: double (opcional)
- visibility_m: double (opcional)
- hasStormRisk: bool (opcional)
- timestampUtc: long

---

## Zonas restringidas

### AirspaceZoneType
- RESTRICTED
- PROHIBITED
- DANGER
- INFO

### AirspaceZone
- id: string
- name: string
- type: AirspaceZoneType
- minAlt_m: double (opcional)
- maxAlt_m: double (opcional)
- polygon: List<GeoPoint>

---

## Modelo para visualización (Unity)

### DisplayModel
- id: string
- worldPosition: Vector3
- labelText: string
- priority: int
- riskLevel: RiskLevel
- showAlert: bool
  
### LogEvent
- timestampUtc
- eventType (ej. RISK_HIGH, GPS_LOST, API_TIMEOUT)
- details (string)

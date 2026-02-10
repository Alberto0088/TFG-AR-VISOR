# Decisiones de diseño (v1)

## D1. Posición del avión propio (Usuario)
- Fuente: GPS de smartphone
- Datos: lat, lon, alt, timestamp
- Frecuencia: 1 Hz

## D2. Comunicación smartphone con Unity
- Envío de datos por red local (Wi-Fi)
- Formato: JSON 

## D3. Uso de la altitud
- Si disponemos de altitud: modo "3D" (tendremos en cuenta altitud)
- Si la altitud no es fiable/no disponible: modo "2D" (no tendremos encuenta la altitud)
    - El usuario tendrá que conocer si se está aplicando el modo 2D para que lo pueda tener en cuenta.

## D4. Suavizado de altitud
- Aplicar suavizado (media posición del móvil) para reducir ruido del GPS

## D5. Conversión de coordenadas
- Separación entre coordenadas geográficas (lat/lon/alt) y coordenadas locales Unity (x/y/z)
- Módulo para ello: CoordinateMapper

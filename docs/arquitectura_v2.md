# Arquitectura del sistema (v2)

## Visión general

El sistema se ha pensando con una arquitectura modular para separar la obtención de datos, el procesamiento y la visualización en realidad aumentada. El objetivo es mantener al prototivo de una manera mantenible y escalable, permitiendo añadir nuevas fuentes de datos como meteorología, zonas restringidas, ett. Sin reescribir nada de la parte visual.

## Componentes principales

- **Fuentes de datos (APIs externas)**: proporcionan información en tiempo real (tráfico aéreo, meteo o avisos).
- **Módulo de obtencion (API Client)**: realiza peticiones, gestiona los errores y convierte los datos obtenidos a un lenguaje común para el resto de módulos.
- **Proveedor de posición propia (Usuario (Avión) – GPS smartphone)**: recibe y valida la posición del usuario (lat/lon/alt) en tiempo real para operar con el tráfico cercano.
- **Proveedor de orientación (HeadPose Provider – XR)**: obtiene la orientación del visor para adaptar la visualización en RA.
- **Módulo de conversión de coordenadas (Coordinate Mapper)**: convierte coordenadas geográficas (lat/lon/alt) a coordenadas locales de Unity (x/y/z) relativas a la posición propia.
- **Módulo de procesamiento**: filtra aeronaves relevantes y calcula el riesgo.
- **Módulo de visualización AR (Unity)**: representa la información en el visor mediante elementos visuales no intrusivos para el piloto.
- **Módulo de registro (logging)**: almacena los eventos, calculos y métricas para una evaluación posterior.
- **Configuración**: define los parámetros del sistema como radio de detección, tiempo de refresco, umbrales concretos, etc.

## Flujo de datos

1. El sistema solicita datos a la API de tráfico de forma periódica.
2. El módulo de obtención normaliza la respuesta y genera "aeronaves" (objetos) internas.
3 El sistema obtiene la posición del avión propio desde el GPS del smartphone y la valida/suaviza.
4 El sistema convierte los datos geográficos (tráfico y posición propia) a coordenadas aparentes en Unity para poder representarlos en RA.
5. El módulo de procesamiento filtra aeronaves por criterios (distancia/altitud) y calcula el riesgo.
6. El módulo AR muestra únicamente la información relevante, priorizando alertas.
7. Se registran eventos y métricas para analizar el comportamiento del prototipo.

## Diagrama de arquitectura 

```mermaid
flowchart LR

  subgraph EXT[Fuentes externas]
    TAPI[API Trafico Aereo]
    WAPI[API Meteorologia]
    ZAPI[API Zonas restringidas]
    GPS[GPS Smartphone]
  end

  CFG[Configuracion]

  subgraph ING[Obtencion y normalizacion]
    ACL[API Client]
    NORM[Modelo interno normalizado]
  end

  subgraph XR[XR Visor]
    HP[HeadPose Provider XR]
  end

  subgraph OWN[Posicion propia y mapeo]
    OWNPROV[Ownship Provider]
    MAP[Coordinate Mapper Geo a Unity]
  end

  subgraph PROC[Procesamiento]
    RISK[Procesamiento filtro y riesgo modo 2D 3D]
  end

  subgraph OUT[Salida]
    UI[Presentacion AR en Unity]
    LOG[Logging y metricas]
  end

  TAPI --> ACL
  WAPI --> ACL
  ZAPI --> ACL
  ACL --> NORM

  GPS --> OWNPROV

  NORM --> MAP
  OWNPROV --> MAP
  MAP --> RISK

  HP --> UI

  RISK --> UI
  RISK --> LOG

  CFG --> ACL
  CFG --> OWNPROV
  CFG --> MAP
  CFG --> RISK
  CFG --> UI

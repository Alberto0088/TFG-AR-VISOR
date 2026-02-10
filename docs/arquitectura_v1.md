# Arquitectura del sistema (v1)

## Visión general

El sistema se ha pensando con una arquitectura modular para separar la obtención de datos, el procesamiento y la visualización en realidad aumentada. El objetivo es mantener al prototivo de una manera mantenible y escalable, permitiendo añadir nuevas fuentes de datos como meteorología, zonas restringidas, ett. Sin reescribir nada de la parte visual.

## Componentes principales

- **Fuentes de datos (APIs externas)**: proporcionan información en tiempo real (tráfico aéreo, meteo o avisos).
- **Módulo de obtencion (API Client)**: realiza peticiones, gestiona los errores y convierte los datos obtenidos a un lenguaje común para el resto de módulos.
- **Módulo de procesamiento**: filtra aeronaves relevantes y calcula el riesgo.
- **Módulo de visualización AR (Unity)**: representa la información en el visor mediante elementos visuales no intrusivos para el piloto.
- **Módulo de registro (logging)**: almacena los eventos, calculos y métricas para una evaluación posterior.
- **Configuración**: define los parámetros del sistema como radio de detección, tiempo de refresco, umbrales concretos, etc.

## Flujo de datos

1. El sistema solicita datos a la API de tráfico de forma periódica.
2. El módulo de obtención normaliza la respuesta y genera "aeronaves" (objetos) internas.
3. El módulo de procesamiento filtra aeronaves por criterios (distancia/altitud) y calcula el riesgo.
4. El módulo AR muestra únicamente la información relevante, priorizando alertas.
5. Se registran eventos y métricas para analizar el comportamiento del prototipo.

## Diagrama de arquitectura 

```mermaid
flowchart LR
    A[API Tráfico Aéreo] --> B[Obtención / API Client]
    B --> C[Modelo interno normalizado]
    C --> D[Procesamiento: filtro + riesgo]
    D --> E[Presentación AR en Unity]
    D --> F[Logging / Métricas]
    G[Configuración] --> B
    G --> D
    G --> E

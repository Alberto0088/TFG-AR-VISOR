# Arquitectura del sistema (v1)

## Visión general

El sistema se ha diseñado con una arquitectura modular para separar la obtención de datos, el procesamiento y la visualización en realidad aumentada. El objetivo es mantener el prototipo mantenible y escalable, permitiendo añadir nuevas fuentes (meteorología, zonas restringidas, etc.) sin reescribir la parte visual.

## Componentes principales

- **Fuentes de datos (APIs externas)**: proporcionan información en tiempo real (tráfico aéreo y, opcionalmente, meteo o avisos).
- **Módulo de ingesta (API Client)**: realiza peticiones, gestiona errores y transforma la respuesta a un modelo interno común.
- **Módulo de procesamiento**: filtra aeronaves relevantes y calcula el nivel de riesgo.
- **Módulo de presentación AR (Unity)**: representa la información en el visor mediante elementos visuales no intrusivos.
- **Módulo de registro (logging)**: almacena eventos y métricas para evaluación posterior.
- **Configuración**: define parámetros del sistema (radio de detección, refresco, umbrales, etc.).

## Flujo de datos

1. El sistema solicita datos a la API de tráfico de forma periódica.
2. El módulo de ingesta normaliza la respuesta y genera objetos internos (aeronaves).
3. El módulo de procesamiento filtra aeronaves por criterios (distancia/altitud) y calcula el riesgo.
4. El módulo AR muestra únicamente la información relevante, priorizando alertas.
5. Se registran eventos y métricas para analizar el comportamiento del prototipo.

## Diagrama de arquitectura (alto nivel)

```mermaid
flowchart LR
    A[API Tráfico Aéreo] --> B[Ingesta / API Client]
    B --> C[Modelo interno normalizado]
    C --> D[Procesamiento: filtro + riesgo]
    D --> E[Presentación AR en Unity]
    D --> F[Logging / Métricas]
    G[Configuración] --> B
    G --> D
    G --> E

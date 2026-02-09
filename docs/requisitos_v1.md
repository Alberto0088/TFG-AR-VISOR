# Sistema de visor en Realidad Aumentada para apoyo a pilotos

Este documento recoge los requisitos del sistema propuesto en el Trabajo de Fin de Grado, cuyo objetivo es el desarrollo de un prototipo funcional de un visor en realidad aumentada orientado a incrementar la seguridad durante el vuelo. El sistema está concebido como un concepto, utilizando un visor de realidad virtual (Meta Quest 3) como soporte experimental. El principal objetivo del sistema es la prevención de posibles colisiones aéreas, mediante la visualización en tiempo real del tráfico cercano, así como la incorporación de información relevante para el piloto sin necesidad de desviar la atención de los instrumentos principales.

---

## Alcance del sistema

El sistema permitirá:

- Obtener información en tiempo real sobre aeronaves cercanas mediante una API externa.
- Analizar dicha información para detectar situaciones de riesgo potencial.
- Mostrar al piloto alertas visuales superpuestas en su campo de visión.
- Proporcionar información adicional de apoyo, como datos meteorológicos o avisos relevantes del entorno.

---

## Requisitos funcionales

### RF-01. Obtención de tráfico aéreo
El sistema deberá obtener información de aeronaves cercanas a partir de una API externa de tráfico aéreo en tiempo real, incluyendo al menos posición, altitud, rumbo y velocidad.

### RF-02. Actualización periódica de datos
El sistema deberá actualizar los datos de tráfico de forma periódica para reflejar cambios en la posición de las aeronaves.

### RF-03. Filtrado de aeronaves relevantes
El sistema deberá filtrar las aeronaves obtenidas en función de criterios configurables, como distancia máxima y diferencia de altitud respecto al avión propio.

### RF-04. Cálculo de riesgo de colisión
El sistema deberá calcular un nivel de riesgo básico para cada aeronave filtrada, teniendo en cuenta la distancia relativa y la tendencia de aproximación.

### RF-05. Clasificación del nivel de riesgo
El sistema deberá clasificar el riesgo de colisión en distintos niveles (por ejemplo: bajo, medio y alto) para facilitar la priorización visual.

### RF-06. Visualización en realidad aumentada
El sistema deberá mostrar en el visor información visual superpuesta sobre las aeronaves relevantes, incluyendo indicadores visuales claros y no intrusivos.

### RF-07. Sistema de alertas visuales
Cuando se detecte un riesgo alto de colisión, el sistema deberá mostrar una alerta visual destacada que permita al piloto identificar rápidamente la situación.

### RF-08. Información meteorológica de apoyo
El sistema podrá mostrar información meteorológica relevante asociada a la ruta o al destino, de forma resumida y comprensible.

### RF-09. Registro de eventos
El sistema deberá registrar información básica sobre los eventos detectados (riesgos, alertas y tiempos) para poder hacer una evaluación y análisis posterior.

---

## Requisitos no funcionales

### RNF-01. Usabilidad
La interfaz deberá diseñarse de forma que minimice la carga cognitiva del piloto y evite la saturación visual.

### RNF-02. Rendimiento
El sistema deberá ser capaz de procesar y mostrar la información con una latencia aceptable para un prototipo, manteniendo una experiencia fluida.

### RNF-03. Robustez ante fallos de conexión
En caso de pérdida de conexión con la API externa, el sistema deberá informar al usuario y continuar funcionando en un modo degradado.

### RNF-04. Modularidad
El sistema deberá estar estructurado en módulos independientes (obtención de datos, procesamiento, visualización) para facilitar su mantenimiento y evolución.

### RNF-05. Portabilidad
El sistema deberá desarrollarse de forma que pueda ejecutarse en distintos entornos compatibles con Unity, sin depender de hardware específico.

### RNF-06. Trazabilidad
Cada funcionalidad implementada deberá poder relacionarse con los requisitos definidos en este documento.

---

## Evolución del documento

Este documento corresponde a una versión inicial (v1) de los requisitos. Podrá actualizarse a lo largo del desarrollo del proyecto conforme se validen las funcionalidades y se identifiquen nuevas necesidades o limitaciones.

## Metodología de desarrollo

El desarrollo de este Trabajo de Fin de Grado se llevará a cabo siguiendo una metodología iterativa y progresiva, orientada a garantizar una correcta organización del trabajo, la trazabilidad de las decisiones tomadas y la validación gradual del prototipo. Dado el carácter experimental del proyecto y su enfoque como sistema conceptual en realidad aumentada, se optará por una combinación de gestión visual mediante Kanban, control de versiones con GitHub y un desarrollo basado en versiones incrementales (alfa, beta y versión final).

### Gestión del trabajo mediante Kanban

Para la planificación y seguimiento de las tareas se utilizará un tablero Kanban implementado en GitHub Projects. Este tablero permitirá visualizar en todo momento el estado del proyecto y organizar el trabajo en distintas columnas, como *Backlog*, *Ready*, *In Progress*, *Review*, *Testing* y *Done*.

Las tareas se definirán en forma de *issues*, incluyendo tanto actividades de análisis y diseño (definición de requisitos, arquitectura del sistema, modelo de datos o decisiones de diseño) como tareas técnicas de implementación y validación. De este modo, el uso del Kanban no se limitará únicamente a la fase de programación, sino que servirá como herramienta central para la organización de todo el proceso de ingeniería del software.

Antes de comenzar la implementación del sistema, se priorizará el cierre de la fase de análisis y diseño, asegurando que los requisitos, la arquitectura y el modelo de datos estén correctamente definidos y documentados. Esta separación clara de fases permitirá reducir errores en etapas posteriores y facilitará la trazabilidad del proyecto.

### Desarrollo incremental y versiones del prototipo

El sistema se desarrollará como un prototipo evolutivo, estructurado en versiones sucesivas que permitirán validar progresivamente las distintas funcionalidades. Para ello se seguirá una estrategia basada en versiones alfa y beta:

- **Versión alfa**: se corresponderá con la primera versión funcional del prototipo. En esta fase se abordará la implementación de los elementos básicos del sistema, como la obtención de datos externos, el tratamiento inicial de la información y una visualización mínima en el visor. El objetivo principal de esta versión será comprobar la viabilidad técnica del enfoque propuesto.

- **Versión beta**: una vez validada la versión alfa, se desarrollará una versión más estable del sistema. En esta etapa se refinarán las funcionalidades existentes, se mejorará la visualización en realidad aumentada y se incorporarán elementos adicionales como alertas visuales, filtrado más avanzado o escenarios de prueba. Esta versión permitirá evaluar el comportamiento global del sistema de forma más cercana a su uso previsto.

- **Versión final**: la versión final integrará las funcionalidades validadas durante las fases anteriores y servirá como resultado definitivo del Trabajo de Fin de Grado. Esta versión se acompañará de una evaluación del sistema y de una reflexión sobre sus limitaciones y posibles líneas de mejora.

Este enfoque incremental permitirá detectar problemas de diseño o limitaciones técnicas en fases tempranas, facilitando la toma de decisiones fundamentadas y reduciendo riesgos durante el desarrollo.

### Uso de GitHub y control de versiones

Todo el proyecto se gestionará mediante un repositorio GitHub, que actuará como eje central del desarrollo. En dicho repositorio se almacenarán tanto el código fuente como la documentación asociada al Trabajo de Fin de Grado, incluyendo los requisitos, la arquitectura del sistema, el modelo de datos y las decisiones de diseño.

El control de versiones mediante Git permitirá registrar la evolución del proyecto, mantener un historial claro de cambios y asegurar la coherencia entre los distintos elementos del sistema. Los avances se reflejarán mediante *commits* periódicos, lo que facilitará la trazabilidad entre las tareas definidas en el tablero Kanban y los cambios realizados en el proyecto.

Además, el uso de GitHub permitirá mantener una estructura organizada del trabajo, separando claramente la documentación del desarrollo técnico y proporcionando una visión global del progreso del proyecto.

### Justificación de la metodología

La metodología propuesta se considera adecuada para el desarrollo de este proyecto, ya que combina flexibilidad, organización y control. El uso de Kanban facilitará la planificación y el seguimiento del trabajo, el desarrollo por versiones incrementales permitirá validar el prototipo de forma progresiva, y el control de versiones mediante GitHub garantizará orden, trazabilidad y reproducibilidad.

En conjunto, esta metodología proporcionará un marco sólido para gestionar la complejidad del sistema propuesto y asegurar un desarrollo coherente y bien estructurado del Trabajo de Fin de Grado.

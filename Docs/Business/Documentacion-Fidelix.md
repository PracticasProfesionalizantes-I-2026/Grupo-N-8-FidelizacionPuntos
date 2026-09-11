# Fidelix

Fidelix surge como respuesta a la falta de un sistema organizado para la gestión de clientes y programas de fidelización, proponiendo una solución simple, centralizada y eficiente que permite administrar la acumulación, vencimiento y canje de puntos, mejorando la experiencia del cliente y optimizando las ventas del negocio.

**INTEGRANTE:** Rodriguez Daniel  

**FACULTAD / CARRERA \- AÑO:** ICES, Desarrollo de software, 2026

**NOMBRE DEL PROYECTO:**  Fidelix \- Sistema de Fidelización de Puntos

# **1\. Requerimientos del Negocio**

## **1.1 Situación actual o Propósito**

Actualmente, el negocio se relaciona con sus clientes de manera informal, utilizando principalmente redes sociales como Instagram y WhatsApp, y en algunos casos registros en planillas o papel. No cuenta con un sistema centralizado para gestionar la información de los clientes ni el historial de sus compras.

En relación con la fidelización, los beneficios y descuentos se aplican según el criterio de cada empleado, lo que genera inconsistencias en la atención. Esto provoca disconformidad en los clientes, ya que algunos reciben descuentos y otros no, generando confusión y pérdida de clientes.

## **1.2 Oportunidad del Negocio**

El desarrollo de una plataforma de fidelización de puntos permite gestionar de forma centralizada la acumulación, vencimiento y canje de beneficios para los clientes. Además, permite tener criterios claros para la asignación de descuentos, evitando problemas en la atención por parte de los empleados. 

Asimismo, incentiva la recurrencia del cliente mediante la acumulación y vencimientos de los puntos, generando nuevas compras y aumentando las ventas del negocio.

## **1.3 Riesgos**

* **Riesgo 1:** Dificultad en la adaptación del sistema a los procesos del negocio  
  (Severidad: Media) Mitigación: Relevamiento previo de necesidades y ajustes progresivos del sistema.  
* **Riesgo 2:** Mal entendimiento del uso del sistema por parte de los clientes  
  (Severidad: Media) Mitigación: Diseño intuitivo y comunicación clara de cómo funcionan los puntos y beneficios.  
* **Riesgo 3:** Errores en la implementación o fallas del sistema en producción  
  (Severidad: Alta) Mitigación: Pruebas previas a la implementación y validación de las funcionalidades principales.  
* **Riesgo 4:** Retraso en el desarrollo del sistema por falta de experiencia del equipo  
  (Severidad: Alta) Mitigación: Capacitación del equipo y planificación de tareas con objetivos claros.  
* **Riesgo 5:** Acumulacion incorrecta de puntos en compras registradas  
  (Severidad: Alta) Mitigación: Validación automática de cálculos y pruebas sobre las reglas de acumulacion  
* **Riesgo 6:** Confusión o reclamos por vencimientos de puntos  
  (Severidad: Media) Mitigación: Mostrar claramente las fechas de vencimientos y el estados de los puntos  
* **Riesgo 7:** Canjes realizados con puntos insuficientes debido a errores en el control del saldo  
  (Severidad: Alta) Mitigación: Validación automática del saldo disponible antes de confirmar el canje

# **2\. Visión de la Solución**

## **2.1 Funciones Principales**

1. Módulo de gestión de clientes  
2. Sistema de acumulación de puntos  
3. Módulo de canje de puntos (descuentos y beneficios)  
4. Panel de historial de compras y actividad del cliente  
5. Sistema de control y asignación de beneficios

# **3\. Contexto del Negocio**

## **3.1 Perfil de los Interesados (Stakeholders)**

|  Stakeholder | Beneficios y Valor Percibido | Actitudes | Funciones de Interes Mayor | Restricciones |
| :---: | ----- | ----- | ----- | ----- |
| **Dueño de Negocio** | Aumentar ventas y retener clientes | Muy interesado en mejorar resultados, pero cauteloso por costos y cambios | Control de clientes, seguimiento de puntos y reportes de ventas | Presupuesto limitado y miedo al cambio |
| **Empleados** | Facilitar su trabajo diario | Resistente al cambio al inicio, pero dispuesto a usarlo si es simple | Carga de compras, asignación de puntos y aplicación de beneficios | Resistencia al cambio, miedo a no saber utilizarlo |
| **Clientes** | Tener beneficios claros, sistema fácil de usar | Expectativa de simplicidad y rapidez, poca tolerancia a sistemas complejos | Consulta de puntos, canje de beneficios y claridad en promociones | No entender cómo funcionan los puntos, perder beneficios |
| **Equipo** | Cumplir con los tiempos y que el sistema funcione correctamente | Comprometido con el desarrollo, pero condicionado por la experiencia y tiempos | Implementación de funcionalidades principales y correcto funcionamiento del sistema | Falta de experiencia y mala definición de requerimientos |

# **4\. Alcance y limitaciones**

## **4.1 Alcance inicial (MVP \- Minimum Viable Product)**

La versión 1.0 del sistema incluirá la gestión de clientes, el registro de compras, la acumulación y canje de puntos, y el acceso mediante un login básico. Esta versión no incluirá características avanzadas o integraciones externas.

## **4.2 Limitaciones y exclusiones (Out of Scope)**

En esta entrega NO se incluirá: 1\) Sistemas de notificaciones automáticas. 2\) Integración con sistemas externos (plataformas de pagos o gestión). 3\) Aplicación móvil. 4\) Reportes avanzados o análisis de comportamiento de clientes.

# **5\. Requerimientos**

## **5.1 Requerimientos Funcionales**

**Clientes:**

* **RF-01:** El sistema debe permitir el ABM de un cliente 
* **RF-02:** El sistema debe permitir inisiar sesion al cliente  
* **RF-03:** El sistema debe permitir gestionar el perfil de un cliente 
* **RF-04:** El sistema debe permitir al cliente visualizar un dashboard con información sobre sus puntos, vencimientos y canjes realizados.
* **RF-05:** El sistema debe permitir al cliente visualizar/consultar beneficios disponibles

**Compras:**

* **RF-06:** El sistema debe permitir registrar compras asociadas a clientes mediante carga manual
* **RF-07:** El sistema debe acreditar automáticamente los puntos correspondientes según la compra realizada.

**Puntos:**

* **RF-08:** El sistema debe gestionar el historial y disponibilidad de puntos de cada cliente.  
* **RF-09:** El sistema debe validar las reglas de negocio correspondientes al canje de puntos.

**Canje:**

* **RF-10:** El sistema debe permitir el canje de puntos por beneficios definidos.  
* **RF-11:** El sistema debe gestionar automáticamente las validaciones y registros correspondientes al proceso de canje.
* **RF-12;** El sistema debe aplicar metodo FIFO en los canjes respecto

**Admin:**
* **RF-13:** El sistema debe permitir iniciar sesion al admin  
* **RF-14:** El sistema debe permitir al administrador visualizar y filtrar el historial de ventas, incluyendo comparaciones entre distintos períodos.  
* **RF-15:** El sistema debe permitir al administrador crear y eliminar un empleado.  
* **RF-16:** El sistema debe permitir al administrador crear y eliminar un cliente.  
* **RF-17:** El sistema debe permitir al administrador gestionar productos y beneficios.
* **RF-18;** El sistema debe permitir al administrador crear o modificar reglas de acumulacion de puntos
* **RF-19;** El sistema debe permitir al administrador realizar auditorias de operaciones
* **RF-20;** El sistema debe permitir al administrador aplicar lapso de vencimiento a los puntos
* **RF-21;** El sistema debe permitir al administrador generar reportes basicos

**Empleado:**

* **RF-22:** El sistema debe permitir iniciar sesion al empleado  
* **RF-23:** El sistema debe permitir visualizar los canjes realizados por los clientes.
* **RF-24:** El sistema debe permitir al empleado corregir errores en la asignación de una compra de un clientes.  
* **RF-25:** El sistema debe permitir al empleado crear una cuenta de cliente.

## **5.2 Requerimientos No Funcionales**

* **RNF-01 (Seguridad):** Las contraseñas de los usuarios deben almacenarse encriptadas en la base de datos.  
* **RNF-02 (Rendimiento):** Las operaciones principales del sistema (registro de compras, consulta de puntos y canje) deben responder en menos de dos segundos.  
* **RNF-03(Usabilidad):** El sistema debe contar con una interfaz simple e intuitiva para facilitar su uso por parte de los empleados.  
* **RNF-04(Arquitectura):** El sistema debe desarrollarse bajo una estructura de 3 capas (presentación, lógica de negocio y acceso a datos).  
* **RNF-05(Documentación):** La API debe estar documentada utilizando herramientas como Swagger/ OpenAPI.  
* **RNF-06(Compatibilidad):** El sistema debe poder ser utilizado desde navegadores web modernos sin requerir instalación adicional.
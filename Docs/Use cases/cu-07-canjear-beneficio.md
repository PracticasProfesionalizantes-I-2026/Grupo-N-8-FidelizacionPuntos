# Caso de Uso: Canjear Beneficio

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-08 (saldo suficiente), RN-09 (método FIFO) y RN-17
> (beneficio activo) **a implementar**; cada caso borde debe contar con su test
> unitario e integración (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-07 |
| **Nombre** | Canjear beneficio |
| **Actor Principal** | Cliente |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Cliente → obtener el beneficio elegido descontando sus puntos correctamente; Empleado → poder entregar el beneficio ya canjeado; Negocio → controlar el consumo de puntos según FIFO |
| **Disparador (Trigger)** | El cliente solicita canjear un beneficio |
| **Prioridad / Frecuencia** | Alta; uso frecuente |
| **Reglas de negocio relacionadas** | RN-08 (saldo suficiente); RN-09 (método FIFO); RN-17 (beneficio activo) |

---

### 1. BREVE DESCRIPCIÓN
Permite al cliente utilizar sus puntos acumulados para obtener un beneficio del
catálogo, descontando los puntos correspondientes mediante el método FIFO.

### 2. PRECONDICIONES
1. El cliente se encuentra registrado y autenticado (Token JWT válido, CU-02).
2. El beneficio solicitado se encuentra activo (**RN-17**).
3. El cliente posee puntos disponibles.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor envía una petición al endpoint `POST /api/canjes` con un JSON que
   contiene el beneficio a canjear (`beneficioId`).
2. La **Capa de Presentación** (`CanjesController.CrearCanje`) valida que el JSON
   sea estructuralmente correcto.
3. La **Capa de Negocio** (`CanjeService.CrearCanjeAsync`) verifica que el
   beneficio esté activo (**RN-17**) y que el cliente posea puntos suficientes
   (**RN-08**).
4. El Sistema (Capa de Negocio, delegando en `LotePuntosService.AplicarFifoAsync` —
   ver CU-23) calcula los puntos a descontar utilizando el método **FIFO** (RN-09).
5. La **Capa de Persistencia** registra el canje y actualiza el saldo de puntos del
   cliente en una única transacción.
6. El Sistema devuelve un código **201 Created** con la confirmación de la
   operación (ID del canje, beneficio, puntos descontados, estado "pendiente").

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Dato obligatorio faltante (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 2 el JSON no incluye `beneficioId`.
  2. El Sistema (Capa de Presentación) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **3a. Beneficio inexistente (HTTP 404 Not Found):**
  1. El sistema detecta que en el Paso 3 el `beneficioId` no corresponde a ningún beneficio registrado.
  2. La Capa de Negocio no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found** con el mensaje: "No hay
     beneficios disponibles". Fin del caso de uso.

* **3b. Beneficio inactivo (HTTP 409 Conflict):**
  1. El sistema detecta que en el Paso 3 el Sistema detecta que el beneficio no está activo, violando
     **RN-17**.
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `BeneficioInactivoException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: "El beneficio
     no pudo ser canjeado". Fin del caso de uso.

* **3c. Saldo insuficiente (HTTP 409 Conflict):**
  1. El sistema detecta que en el Paso 3 el Sistema detecta que el cliente no posee puntos suficientes
     para el beneficio, violando **RN-08**.
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `SaldoInsuficienteException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: "Saldo
     insuficiente para hacer canje". Fin del caso de uso.

* **5a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. El sistema detecta que en el Paso 5 la Capa de Persistencia no puede registrar el canje o
     actualizar el saldo (ej. falla de conexión).
  2. El Sistema revierte la transacción y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de
     uso.

### 5. SUB-VARIACIONES (opcional)
_No aplica: el canje se realiza por un único canal (API del cliente); el canje
presencial en punto de venta lo gestiona el empleado (ver CU-11)._

### 6. POSTCONDICIONES
1. El canje queda registrado en la tabla `Canjes` con estado "pendiente".
2. El saldo de puntos del cliente queda actualizado, descontando los lotes más
   antiguos según **RN-09**.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Confirmación de persistencia exitosa del nuevo recurso Canje. |
| `400` | Bad Request | Fallo en la validación de esquema o sintaxis del JSON recibido. |
| `404` | Not Found | Inexistencia del beneficio referenciado en la Capa de Persistencia. |
| `409` | Conflict | Violación de RN-17 (beneficio inactivo) o RN-08 (saldo insuficiente). |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** presencia de `beneficioId` y formato del
  JSON (model binding).
- **Verificación (Negocio, → 404/409):** existencia y estado activo del beneficio
  (RN-17), saldo suficiente del cliente (RN-08) y aplicación del método FIFO
  (RN-09) al calcular los lotes a descontar.

### Matriz de trazabilidad CU-07 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `CrearCanjeAsync_WithSufficientBalance_CreatesCanjeAndUpdatesSaldo` | `CrearCanje_WithValidData_Returns201Created` |
| 2a. Dato obligatorio faltante | `400 Bad Request` | — (validación de esquema) | `CrearCanje_WithMissingBeneficioId_Returns400BadRequest` |
| 3a. Beneficio inexistente | `404 Not Found` | `CrearCanjeAsync_WithNonExistentBeneficio_ThrowsBeneficioNotFoundException` | `CrearCanje_WithNonExistentBeneficio_Returns404NotFound` |
| 3b. Beneficio inactivo | `409 Conflict` | `CrearCanjeAsync_WithInactiveBeneficio_ThrowsBeneficioInactivoException` | `CrearCanje_WithInactiveBeneficio_Returns409Conflict` |
| 3c. Saldo insuficiente | `409 Conflict` | `CrearCanjeAsync_WithInsufficientBalance_ThrowsSaldoInsuficienteException` | `CrearCanje_WithInsufficientBalance_Returns409Conflict` |
| 5a. Error interno de persistencia | `500 Internal Server Error` | `CrearCanjeAsync_WhenRepositoryFails_ThrowsPersistenceException` | `CrearCanje_WhenPersistenceFails_Returns500InternalServerError` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. El cálculo
> FIFO se cubre en detalle en la matriz de CU-23.

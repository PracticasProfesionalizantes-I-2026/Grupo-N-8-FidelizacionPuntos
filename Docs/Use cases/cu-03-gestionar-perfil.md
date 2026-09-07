# Caso de Uso: Gestionar Perfil

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-04 (documento inmutable) y RN-01 (email único) **a
> implementar**; cada caso borde debe contar con su test unitario e integración
> (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-03 |
| **Nombre** | Gestionar perfil |
| **Actor Principal** | Cliente |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Cliente → mantener sus datos de contacto actualizados; Sistema → conservar la unicidad de email y la inmutabilidad del documento como identificador |
| **Disparador (Trigger)** | El cliente solicita acceder a su perfil |
| **Prioridad / Frecuencia** | Media; uso ocasional |
| **Reglas de negocio relacionadas** | RN-04 (documento no modificable); RN-01 (email único) |

---

### 1. BREVE DESCRIPCIÓN
Permite al cliente visualizar y modificar sus datos personales (nombre, email,
contraseña), preservando el documento como identificador inmutable.

### 2. PRECONDICIONES
1. El actor debe poseer un estado de autenticación activo (Token JWT válido,
   CU-02).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición `GET /api/clientes/me` y luego `PUT
   /api/clientes/me` con un JSON que contiene los datos a actualizar (`nombre`,
   `email`; opcionalmente `password`).
2. La **Capa de Presentación** (`ClientesController.ActualizarPerfil`) valida que
   el JSON sea estructuralmente correcto (`[Required]`/`[MaxLength]` sobre
   `ClienteUpdateDTO`) y descarta cualquier intento de modificar `documento`.
3. La **Capa de Negocio** (`ClienteService.ActualizarPerfilAsync`) verifica que el
   nuevo email, si cambió, siga siendo único en el sistema (**RN-01**).
4. La **Capa de Persistencia** actualiza el registro del cliente en la tabla
   `Clientes`.
5. El Sistema devuelve un código **200 OK** con los datos actualizados del cliente.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Dato inválido (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 2 el JSON contiene campos con formato incorrecto o excede la
     longitud máxima permitida.
  2. El Sistema (Capa de Presentación) rechaza la petición por validación de
     esquema.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje: "Los datos
     ingresados son inválidos". Fin del caso de uso.

* **2b. Intento de modificar el documento (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 2 el JSON incluye un campo `documento` distinto al almacenado,
     violando **RN-04**.
  2. El Sistema (Capa de Presentación) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje: "El
     documento de identidad no puede ser modificado". Fin del caso de uso.

* **3a. Email ya registrado por otro cliente (HTTP 409 Conflict):**
  1. El sistema detecta que en el Paso 3 el nuevo email ya pertenece a otro cliente, violando **RN-01**.
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `EmailDuplicadoException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: "El email
     ingresado ya se encuentra registrado". Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
_No aplica: el módulo de perfil se gestiona por un único canal (API) sin variantes
relevantes de mecanismo._

### 6. POSTCONDICIONES
1. Los datos del cliente quedan actualizados en la tabla `Clientes`.
2. El documento de identidad permanece sin cambios respecto del valor original.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al consultar o actualizar el perfil. |
| `400` | Bad Request | Datos inválidos o intento de modificar el documento (RN-04). |
| `409` | Conflict | Violación de RN-01: email ya registrado por otro cliente. |

### Matriz de trazabilidad CU-03 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `ActualizarPerfilAsync_WithValidData_UpdatesAndReturnsCliente` | `ActualizarPerfil_WithValidData_Returns200OK` |
| 2a. Dato inválido | `400 Bad Request` | — (validación de esquema) | `ActualizarPerfil_WithInvalidField_Returns400BadRequest` |
| 2b. Intento de modificar documento | `400 Bad Request` | `ActualizarPerfilAsync_WhenDocumentoChanged_ThrowsValidationException` | `ActualizarPerfil_WhenDocumentoChanged_Returns400BadRequest` |
| 3a. Email duplicado | `409 Conflict` | `ActualizarPerfilAsync_WhenEmailAlreadyExists_ThrowsEmailDuplicadoException` | `ActualizarPerfil_WhenEmailAlreadyExists_Returns409Conflict` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.

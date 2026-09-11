# Caso de Uso: Gestionar Perfil

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-04 (documento inmutable), RN-01 (email único) y RN-14
> (cliente dado de baja no acumula ni canjea) **a implementar**; cada caso
> borde debe contar con su test unitario e integración (ver matriz de
> trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-03 |
| **Nombre** | Gestionar perfil |
| **Actor Principal** | Cliente |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Cliente → mantener sus datos de contacto actualizados, o darse de baja si ya no quiere usar el sistema; Sistema → conservar la unicidad de email y la inmutabilidad del documento como identificador |
| **Disparador (Trigger)** | El cliente solicita acceder a su perfil |
| **Prioridad / Frecuencia** | Media; uso ocasional |
| **Reglas de negocio relacionadas** | RN-04 (documento no modificable); RN-01 (email único); RN-14 (baja lógica, no elimina el historial) |

---

### 1. BREVE DESCRIPCIÓN
Permite al cliente visualizar y modificar sus datos personales (nombre,
apellido, teléfono, email, contraseña), preservando el documento como
identificador inmutable, y darse de baja voluntariamente del sistema.

### 2. PRECONDICIONES
1. El actor debe poseer un estado de autenticación activo (Token JWT válido,
   CU-02).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200/204)
1. El Actor envía una petición al endpoint correspondiente a la operación
   elegida: `GET /api/clientes/me` (ver), `PUT /api/clientes/me` (modificar) o
   `DELETE /api/clientes/me` (baja de cuenta).
2. La **Capa de Presentación** (`ClientesController.ActualizarPerfil` /
   `DarseDeBaja`) valida que el JSON (en modificación) sea estructuralmente
   correcto (`[Required]`/`[MaxLength]` sobre `ClienteUpdateDTO`) y descarta
   cualquier intento de modificar `documento`.
3. La **Capa de Negocio** (`ClienteService.ActualizarPerfilAsync` /
   `DarseDeBajaAsync`) verifica que el nuevo email, si cambió, siga siendo
   único en el sistema (**RN-01**).
4. La **Capa de Persistencia** actualiza el registro del cliente en la tabla
   `Clientes` (datos, contraseña, o marca de `Activo = false` en la baja).
5. El Sistema devuelve un código **200 OK** (ver/modificar) o **204 No
   Content** (baja) con el resultado de la operación.

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
1. **Cambio de contraseña:** la interfaz lo presenta como una pantalla separada
   ("Cambiar contraseña"), pero técnicamente reutiliza el mismo
   `PUT /api/clientes/me` enviando únicamente el campo `password`; no requiere
   un endpoint propio.

### 6. POSTCONDICIONES
1. Los datos del cliente quedan actualizados en la tabla `Clientes`, o el
   cliente queda con `Activo = false` en la baja.
2. El documento de identidad permanece sin cambios respecto del valor original.
3. Un cliente dado de baja queda inhabilitado para acumular (CU-10) y canjear
   (CU-07, CU-11) puntos (**RN-14**), pero su historial de movimientos y
   canjes se conserva sin cambios.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al consultar o actualizar el perfil. |
| `204` | No Content | Éxito en la baja lógica de la cuenta (sin cuerpo en la respuesta). |
| `400` | Bad Request | Datos inválidos o intento de modificar el documento (RN-04). |
| `409` | Conflict | Violación de RN-01: email ya registrado por otro cliente. |

### Matriz de trazabilidad CU-03 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal (ver/modificar) | `200 OK` | `ActualizarPerfilAsync_WithValidData_UpdatesAndReturnsCliente` | `ActualizarPerfil_WithValidData_Returns200OK` |
| Flujo principal (baja) | `204 No Content` | `DarseDeBajaAsync_WithAuthenticatedCliente_DeactivatesCliente` | `DarseDeBaja_WithAuthenticatedCliente_Returns204NoContent` |
| 2a. Dato inválido | `400 Bad Request` | — (validación de esquema) | `ActualizarPerfil_WithInvalidField_Returns400BadRequest` |
| 2b. Intento de modificar documento | `400 Bad Request` | `ActualizarPerfilAsync_WhenDocumentoChanged_ThrowsValidationException` | `ActualizarPerfil_WhenDocumentoChanged_Returns400BadRequest` |
| 3a. Email duplicado | `409 Conflict` | `ActualizarPerfilAsync_WhenEmailAlreadyExists_ThrowsEmailDuplicadoException` | `ActualizarPerfil_WhenEmailAlreadyExists_Returns409Conflict` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.

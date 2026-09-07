# Caso de Uso: Crear Cliente (Empleado)

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Regla de negocio RN-01 (documento único) **a implementar**; cada caso borde debe
> contar con su test unitario e integración (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-12 |
| **Nombre** | Crear cliente (desde punto de venta) |
| **Actor Principal** | Empleado |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Empleado → poder registrar en el momento a un cliente nuevo sin frenar la venta; Cliente → sumarse al programa de fidelización en el local; Negocio → captar clientes también desde el punto de venta físico |
| **Disparador (Trigger)** | El empleado atiende a un cliente que no se encuentra registrado |
| **Prioridad / Frecuencia** | Media; uso frecuente |
| **Reglas de negocio relacionadas** | RN-01 (documento único en el sistema) |

---

### 1. BREVE DESCRIPCIÓN
Permite al empleado registrar a un nuevo cliente en el sistema desde el punto de
venta, con saldo de puntos inicial en cero.

### 2. PRECONDICIONES
1. El empleado se encuentra autenticado (Token JWT válido, CU-09).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor envía una petición al endpoint `POST /api/clientes` con un JSON que
   contiene los datos del cliente (`nombre`, `documento`, `email`).
2. La **Capa de Presentación** (`ClientesController.CrearDesdePOS`) valida que el
   JSON sea estructuralmente correcto y que los campos requeridos estén presentes.
3. La **Capa de Negocio** (`ClienteService.CrearClienteDesdePOSAsync`) verifica
   que el documento no esté registrado (**RN-01**).
4. La **Capa de Persistencia** genera un nuevo `Id` (GUID), guarda el registro en
   la tabla `Clientes` y asigna un saldo inicial de puntos en cero.
5. El Sistema devuelve un código **201 Created** con la confirmación de la
   creación del cliente.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Datos inválidos o incompletos (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 2 el JSON no incluye `nombre` o `documento`, o el formato es
     incorrecto.
  2. El Sistema (Capa de Presentación) rechaza la petición por error de
     validación.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje: "Los datos
     ingresados son inválidos o no existen". El flujo retorna al Paso 1.

* **3a. Cliente ya existe (HTTP 409 Conflict):**
  1. El sistema detecta que en el Paso 3 el documento ingresado ya pertenece a un cliente registrado,
     violando **RN-01**.
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `ClienteDuplicadoException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: "El documento
     ingresado pertenece a un cliente activo". El flujo retorna al Paso 1.

### 5. SUB-VARIACIONES (opcional)
_No aplica: el alta desde el punto de venta se realiza por un único canal
(terminal del empleado)._

### 6. POSTCONDICIONES
1. El cliente queda registrado en el sistema con saldo de puntos inicial en cero,
   disponible de inmediato para acumular puntos (CU-10).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Confirmación de persistencia exitosa del nuevo recurso Cliente. |
| `400` | Bad Request | Fallo en la validación de esquema o campos faltantes. |
| `409` | Conflict | Violación de RN-01: documento ya registrado. |

### Matriz de trazabilidad CU-12 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `CrearClienteDesdePOSAsync_WithValidData_CreatesAndReturnsCliente` | `CrearClientePOS_WithValidData_Returns201Created` |
| 2a. Datos inválidos | `400 Bad Request` | — (validación de esquema) | `CrearClientePOS_WithInvalidData_Returns400BadRequest` |
| 3a. Cliente ya existe | `409 Conflict` | `CrearClienteDesdePOSAsync_WhenDocumentoExists_ThrowsClienteDuplicadoException` | `CrearClientePOS_WhenDocumentoExists_Returns409Conflict` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.

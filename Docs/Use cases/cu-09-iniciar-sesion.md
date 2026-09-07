# Caso de Uso: Iniciar Sesión (Empleado)

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Regla de negocio RN-11 (control de acceso por rol) **a implementar**; cada caso
> borde debe contar con su test unitario e integración (ver matriz de
> trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-09 |
| **Nombre** | Iniciar sesión (Empleado) |
| **Actor Principal** | Empleado |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Empleado → acceder a las funcionalidades operativas del punto de venta; Negocio → garantizar que solo empleados activos operen puntos y canjes |
| **Disparador (Trigger)** | El empleado solicita iniciar sesión |
| **Prioridad / Frecuencia** | Alta; uso muy frecuente (cada turno) |
| **Reglas de negocio relacionadas** | RN-11 (acceso limitado a las funcionalidades del rol) |

---

### 1. BREVE DESCRIPCIÓN
Permite al empleado autenticarse en el sistema para acceder a sus funcionalidades
operativas (acumulación de puntos, canje presencial, alta de clientes).

### 2. PRECONDICIONES
1. El empleado se encuentra registrado en el sistema (CU-15) y activo.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `POST /api/auth/login` con un JSON que
   contiene sus credenciales (`email`, `password`).
2. La **Capa de Presentación** (`AuthController.LoginEmpleado`) valida que el JSON
   sea estructuralmente correcto.
3. La **Capa de Negocio** (`AuthService.LoginEmpleadoAsync`) valida las
   credenciales y verifica que la cuenta esté activa.
4. El Sistema genera un token JWT firmado con los claims del empleado (Id, rol
   `Empleado`), limitando su alcance según **RN-11**.
5. El Sistema devuelve un código **200 OK** con el token y los datos básicos del
   empleado.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Dato obligatorio faltante (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 2 el JSON no incluye `email` o `password`.
  2. El Sistema (Capa de Presentación) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **3a. Credenciales incorrectas (HTTP 401 Unauthorized):**
  1. El sistema detecta que en el Paso 3 las credenciales no coinciden con ningún empleado registrado.
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `CredencialesInvalidasException`.
  3. El Sistema devuelve un código **401 Unauthorized** con el mensaje: "Usuario o
     contraseña incorrectos". Fin del caso de uso.

* **3b. Cuenta inactiva (HTTP 403 Forbidden):**
  1. El sistema detecta que en el Paso 3 el Sistema detecta que la cuenta del empleado está inactiva
     (CU-15, **RN-15**).
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `CuentaInactivaException`.
  3. El Sistema devuelve un código **403 Forbidden** con el mensaje: "La cuenta se
     encuentra inactiva. Contacte al administrador". Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
_No aplica: el login del empleado se realiza por un único canal (terminal de punto
de venta / panel operativo)._

### 6. POSTCONDICIONES
1. El empleado queda autenticado en el sistema, con un token JWT cuyo alcance está
   restringido a las funcionalidades de su rol.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Autenticación exitosa; se devuelve el token JWT. |
| `400` | Bad Request | Fallo en la validación de esquema o campos faltantes. |
| `401` | Unauthorized | Credenciales incorrectas. |
| `403` | Forbidden | Cuenta de empleado inactiva (RN-15). |

### Matriz de trazabilidad CU-09 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `LoginEmpleadoAsync_WithValidCredentials_ReturnsJwtToken` | `LoginEmpleado_WithValidCredentials_Returns200OK` |
| 2a. Dato obligatorio faltante | `400 Bad Request` | — (validación de esquema) | `LoginEmpleado_WithMissingField_Returns400BadRequest` |
| 3a. Credenciales incorrectas | `401 Unauthorized` | `LoginEmpleadoAsync_WithWrongPassword_ThrowsCredencialesInvalidasException` | `LoginEmpleado_WithWrongPassword_Returns401Unauthorized` |
| 3b. Cuenta inactiva | `403 Forbidden` | `LoginEmpleadoAsync_WhenAccountInactive_ThrowsCuentaInactivaException` | `LoginEmpleado_WhenAccountInactive_Returns403Forbidden` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.

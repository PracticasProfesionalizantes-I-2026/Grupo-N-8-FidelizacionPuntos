# Caso de Uso: Iniciar Sesión (Admin)

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Regla de negocio RN-11 (control de acceso por rol) **a implementar**; cada caso
> borde debe contar con su test unitario e integración (ver matriz de
> trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-13 |
| **Nombre** | Iniciar sesión (Admin) |
| **Actor Principal** | Admin |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Admin → acceder a las funcionalidades de gestión y configuración; Negocio → asegurar que solo administradores autorizados operen sobre la configuración del sistema |
| **Disparador (Trigger)** | El admin solicita iniciar sesión |
| **Prioridad / Frecuencia** | Media; uso frecuente |
| **Reglas de negocio relacionadas** | RN-11 (acceso a módulos de configuración restringido al rol Admin) |

---

### 1. BREVE DESCRIPCIÓN
Permite al administrador autenticarse en el sistema para acceder a las
funcionalidades de gestión y configuración (ABM, reglas de acumulación, reportes,
auditoría).

### 2. PRECONDICIONES
1. El admin se encuentra registrado en el sistema.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `POST /api/auth/login` con un JSON que
   contiene sus credenciales (`email`, `password`).
2. La **Capa de Presentación** (`AuthController.LoginAdmin`) valida que el JSON
   sea estructuralmente correcto.
3. La **Capa de Negocio** (`AuthService.LoginAdminAsync`) valida las credenciales
   contra el hash almacenado.
4. El Sistema genera un token JWT firmado con los claims del admin (Id, rol
   `Admin`), habilitando el acceso a los módulos de configuración según **RN-11**.
5. El Sistema devuelve un código **200 OK** con el token y los datos básicos del
   admin.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Dato obligatorio faltante (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el JSON no incluye `email` o `password`.
  2. El Sistema (Capa de Presentación) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **3a. Credenciales incorrectas (HTTP 401 Unauthorized):**
  1. Si en el Paso 3 las credenciales no coinciden con ningún admin registrado.
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `CredencialesInvalidasException`.
  3. El Sistema devuelve un código **401 Unauthorized** con el mensaje: "Usuario o
     contraseña incorrectos". Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
_No aplica: el login del admin se realiza por un único canal (panel de
administración web)._

### 6. POSTCONDICIONES
1. El admin queda autenticado en el sistema, con un token JWT que habilita el
   acceso a los módulos de configuración.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Autenticación exitosa; se devuelve el token JWT. |
| `400` | Bad Request | Fallo en la validación de esquema o campos faltantes. |
| `401` | Unauthorized | Credenciales incorrectas. |

### Matriz de trazabilidad CU-13 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `LoginAdminAsync_WithValidCredentials_ReturnsJwtToken` | `LoginAdmin_WithValidCredentials_Returns200OK` |
| 2a. Dato obligatorio faltante | `400 Bad Request` | — (validación de esquema) | `LoginAdmin_WithMissingField_Returns400BadRequest` |
| 3a. Credenciales incorrectas | `401 Unauthorized` | `LoginAdminAsync_WithWrongPassword_ThrowsCredencialesInvalidasException` | `LoginAdmin_WithWrongPassword_Returns401Unauthorized` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.

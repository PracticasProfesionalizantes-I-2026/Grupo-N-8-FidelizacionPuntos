# Caso de Uso: Iniciar Sesión (Cliente)

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Regla de negocio RN-03 (bloqueo temporal por intentos fallidos) **implementada**
> (ver sección 7); cada caso borde cuenta con su test unitario e integración (ver
> matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-02 |
| **Nombre** | Iniciar sesión (Cliente) |
| **Actor Principal** | Cliente |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Cliente → acceder a sus funcionalidades (saldo, canjes); Sistema → garantizar que solo cuentas válidas y activas obtengan un token |
| **Disparador (Trigger)** | El cliente solicita iniciar sesión |
| **Prioridad / Frecuencia** | Alta; uso muy frecuente (cada sesión) |
| **Reglas de negocio relacionadas** | RN-03 (bloqueo temporal tras intentos fallidos), RN-14 (baja lógica) |
| **Requerimientos no funcionales** | RNF-01 (la contraseña se almacena como hash PBKDF2-SHA256 con salt; el login la verifica contra ese hash, nunca en texto plano) |

---

### 1. BREVE DESCRIPCIÓN
Permite al cliente autenticarse en el sistema mediante credenciales (email/usuario y
contraseña) para obtener un token de acceso (JWT) y acceder a sus funcionalidades.

### 2. PRECONDICIONES
1. El cliente debe encontrarse registrado (CU-01) en la Capa de Persistencia.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `POST /api/auth/login` con un JSON que
   contiene sus credenciales (`email`, `password`).
2. La **Capa de Presentación** (`AuthController.LoginCliente`) valida que el JSON
   sea estructuralmente correcto y que los campos requeridos estén presentes.
3. La **Capa de Negocio** (`AuthService.LoginClienteAsync`) verifica, en este orden:
   que el email exista, que la cuenta esté activa, que no esté bloqueada (RN-03) y
   que la contraseña coincida con el hash almacenado (RNF-01). Las cuentas inactivas
   o bloqueadas se rechazan antes de mirar la contraseña, para no modificar su
   contador de intentos.
4. El Sistema reinicia el contador de intentos fallidos y limpia el bloqueo, y
   genera un token JWT firmado (HS256) con los claims del cliente (Id, rol
   `Cliente`) y su fecha de expiración.
5. El Sistema devuelve un código **200 OK** con el token y los datos básicos del
   cliente.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Dato obligatorio faltante (HTTP 400 Bad Request):**
  1. El Sistema detecta que en el Paso 2 el JSON no incluye `email` o `password`.
  2. El Sistema (Capa de Presentación) rechaza la petición por error de validación.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **3a. Credenciales incorrectas (HTTP 401 Unauthorized):**
  1. El Sistema detecta que en el Paso 3 el email no existe o la contraseña no coincide con el hash
     almacenado.
  2. Si el email existe y la contraseña es incorrecta, el Sistema (Capa de Negocio)
     incrementa el contador de intentos fallidos, evalúa la regla **RN-03** (ver
     sección 7) y guarda el cambio. En ambos casos lanza la excepción de dominio
     `CredencialesInvalidasException`.
  3. Por seguridad, el mensaje es el mismo si el email no existe o si la contraseña
     es incorrecta, para no revelar qué emails están registrados (enumeración de
     usuarios).
  4. El Sistema devuelve un código **401 Unauthorized** con el mensaje: "Usuario o
     contraseña incorrectos". Fin del caso de uso.

* **3b. Cuenta bloqueada por intentos fallidos (HTTP 403 Forbidden):**
  1. El Sistema detecta que en el Paso 3 la cuenta tiene un bloqueo temporal vigente
     (`BloqueadoHasta` posterior a la hora actual, **RN-03**). El intento no suma
     fallos ni extiende el bloqueo.
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `CuentaBloqueadaException`.
  3. El Sistema devuelve un código **403 Forbidden** con el mensaje: "La cuenta se
     encuentra bloqueada temporalmente. Intente nuevamente más tarde". Fin del caso
     de uso.

* **3c. Cuenta inactiva (HTTP 403 Forbidden):**
  1. El Sistema detecta que en el Paso 3 la cuenta del cliente fue dada de baja
     (CU-15, **RN-14**).
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `CuentaInactivaException`.
  3. El Sistema devuelve un código **403 Forbidden** con el mensaje: "La cuenta se
     encuentra inactiva. Contacte al administrador". Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. El actor puede iniciar sesión desde la app web en este caso el
   esquema del cuerpo y el resultado (`200 OK` con token JWT) son idénticos.

### 6. POSTCONDICIONES
1. El cliente queda autenticado en el sistema y en posesión de un token JWT válido.
2. El contador de intentos fallidos de la cuenta queda reiniciado en cero y el
   bloqueo temporal (`BloqueadoHasta`) queda limpio.

### 7. DETALLE DE LA REGLA RN-03 (bloqueo temporal)

| Parámetro | Valor |
| --- | --- |
| Umbral de bloqueo | A partir del **3.er** intento fallido consecutivo |
| Espera | **30 s × 2^(fallos − 3)** (se duplica con cada fallo) |
| Tope de espera | **15 minutos** |
| Intentos durante el bloqueo | Se rechazan con 403 y **no suman** fallos |
| Reinicio del contador | Solo con un login exitoso |

| Fallos acumulados | 1-2 | 3 | 4 | 5 | 6 | 7 | 8 o más |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Bloqueo | — | 30 s | 1 min | 2 min | 4 min | 8 min | 15 min (tope) |

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Autenticación exitosa; se devuelve el token JWT. |
| `400` | Bad Request | Fallo en la validación de esquema o campos faltantes. |
| `401` | Unauthorized | Credenciales incorrectas. |
| `403` | Forbidden | Cuenta bloqueada (RN-03) o inactiva (RN-14). |

### Matriz de trazabilidad CU-02 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `LoginClienteAsync_WithValidCredentials_ReturnsJwtToken` | `LoginCliente_WithValidCredentials_Returns200OK` |
| 2a. Dato obligatorio faltante | `400 Bad Request` | — (validación de esquema) | `LoginCliente_WithMissingField_Returns400BadRequest` |
| 3a. Credenciales incorrectas | `401 Unauthorized` | `LoginClienteAsync_WithWrongPassword_ThrowsCredencialesInvalidasException` | `LoginCliente_WithWrongPassword_Returns401Unauthorized` |
| 3b. Cuenta bloqueada | `403 Forbidden` | `LoginClienteAsync_WhenAccountLocked_ThrowsCuentaBloqueadaException` | `LoginCliente_WhenAccountLocked_Returns403Forbidden` |
| 3c. Cuenta inactiva | `403 Forbidden` | `LoginClienteAsync_WhenAccountInactive_ThrowsCuentaInactivaException` | `LoginCliente_WhenAccountInactive_Returns403Forbidden` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.

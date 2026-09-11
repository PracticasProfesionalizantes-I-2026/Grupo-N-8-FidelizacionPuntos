# Caso de Uso: Recuperar Contraseña

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3), generalizada para los tres
> roles con credenciales (Cliente, Empleado, Admin) mediante sub-variaciones,
> en vez de triplicar el archivo.
> Reglas de negocio RN-27 (código de recuperación de un solo uso y con
> vencimiento corto) y RN-28 (la recuperación del empleado se envía al correo
> del admin) **a implementar**; cada caso borde debe contar con su test
> unitario e integración (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-25 |
| **Nombre** | Recuperar contraseña |
| **Actor Principal** | Cliente, Empleado o Admin |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Actor → recuperar el acceso a su cuenta sin depender de soporte técnico; Negocio → que el mecanismo no exponga información de cuentas existentes ni quede abierto a abuso |
| **Disparador (Trigger)** | El actor olvidó su contraseña y solicita recuperarla desde la pantalla de login |
| **Prioridad / Frecuencia** | Baja; uso ocasional |
| **Reglas de negocio relacionadas** | RN-27 (código de un solo uso, con vencimiento corto); RN-28 (empleado: código enviado al correo del admin) |

---

### 1. BREVE DESCRIPCIÓN
Permite a un actor (cliente, empleado o admin) recuperar el acceso a su cuenta
mediante un código temporal enviado por email, para luego definir una nueva
contraseña.

### 2. PRECONDICIONES
1. El actor posee una cuenta registrada y activa en el sistema.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición `POST /api/auth/recuperar-contrasena` con su
   email (o documento, según el rol).
2. La **Capa de Presentación** (`AuthController.SolicitarRecuperacion`) valida
   que el JSON sea estructuralmente correcto.
3. La **Capa de Negocio** (`AuthService.SolicitarRecuperacionAsync`) genera un
   código temporal de un solo uso, válido por un plazo corto (**RN-27**), y lo
   envía por email al destinatario que corresponda según el rol (ver
   Sub-variaciones).
4. El Actor envía una segunda petición `POST
   /api/auth/recuperar-contrasena/confirmar` con el email, el código recibido
   y la nueva contraseña.
5. La **Capa de Negocio** (`AuthService.ConfirmarRecuperacionAsync`) valida el
   código (vigente, no usado previamente) y actualiza la contraseña.
6. El Sistema devuelve un código **200 OK** confirmando el cambio de
   contraseña.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **3a. Cuenta inexistente o inactiva (HTTP 200 OK, sin envío de código):**
  1. El sistema detecta que en el Paso 3 el email/documento no corresponde a
     ninguna cuenta activa.
  2. El Sistema **no informa esta condición al actor**, para no revelar qué
     cuentas existen en el sistema.
  3. El Sistema devuelve igualmente un código **200 OK** con un mensaje
     genérico ("Si el dato ingresado corresponde a una cuenta, se envió un
     código"), sin enviar ningún email. Fin del caso de uso.

* **5a. Código inválido, expirado o ya utilizado (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 5 el código no coincide, ya venció o ya
     fue utilizado, violando **RN-27**.
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `CodigoRecuperacionInvalidoException`.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje: "El
     código ingresado es inválido o expiró". El flujo retorna al Paso 1.

* **5b. Nueva contraseña inválida (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 5 la nueva contraseña no cumple la
     política mínima (**RN-02**).
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `PasswordInvalidaException`.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje: "La
     contraseña no cumple los requisitos mínimos". El flujo retorna al Paso 4.

### 5. SUB-VARIACIONES
1. **Cliente:** el código se envía al email registrado del propio cliente.
2. **Admin:** el código se envía al email registrado del propio admin.
3. **Empleado:** por **RN-28**, el código se envía al email del **admin**
   (no al del empleado), que es quien se lo transmite de forma presencial o
   por otro canal; esto evita que una cuenta de correo comprometida del
   empleado sea suficiente para tomar control de su usuario en el sistema.

### 6. POSTCONDICIONES
1. La contraseña de la cuenta queda actualizada.
2. El código utilizado queda invalidado y no puede reutilizarse.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al solicitar el código (exista o no la cuenta) o al confirmar la nueva contraseña. |
| `400` | Bad Request | Código inválido/expirado/usado (RN-27), o nueva contraseña inválida (RN-02). |

### Matriz de trazabilidad CU-25 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `ConfirmarRecuperacionAsync_WithValidCode_UpdatesPassword` | `RecuperarContrasena_WithValidCode_Returns200OK` |
| 3a. Cuenta inexistente/inactiva | `200 OK` (sin envío) | `SolicitarRecuperacionAsync_WithUnknownAccount_DoesNotSendEmail` | `SolicitarRecuperacion_WithUnknownAccount_Returns200OKWithoutLeakingExistence` |
| 5a. Código inválido/expirado/usado | `400 Bad Request` | `ConfirmarRecuperacionAsync_WithInvalidCode_ThrowsCodigoRecuperacionInvalidoException` | `ConfirmarRecuperacion_WithInvalidCode_Returns400BadRequest` |
| 5b. Contraseña inválida | `400 Bad Request` | `ConfirmarRecuperacionAsync_WithWeakPassword_ThrowsPasswordInvalidaException` | `ConfirmarRecuperacion_WithWeakPassword_Returns400BadRequest` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.

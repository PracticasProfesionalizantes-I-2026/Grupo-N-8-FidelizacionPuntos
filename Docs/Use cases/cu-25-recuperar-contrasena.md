# Caso de Uso: Recuperar Contraseña

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3), generalizada para los tres
> roles con credenciales (Cliente, Empleado, Admin) mediante sub-variaciones,
> en vez de triplicar el archivo.
> Reglas de negocio RN-27 (código de recuperación de un solo uso y con
> vencimiento corto) y RN-28 (la recuperación del empleado se envía al correo
> del admin) **implementadas**; cada caso borde cuenta con su test
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
   email y el **rol** de la cuenta (`Cliente`, `Empleado` o `Admin`), que lo indica
   la pantalla de login desde la que se pidió la recuperación.
2. La **Capa de Presentación** (`AuthPersonalController.SolicitarRecuperacion`) valida
   que el JSON sea estructuralmente correcto.
3. La **Capa de Negocio** (`RecuperacionService.SolicitarRecuperacionAsync`) genera un
   código temporal de un solo uso, válido por un plazo corto (**RN-27**), y lo
   envía por email al destinatario que corresponda según el rol (ver
   Sub-variaciones).
4. El Actor envía una segunda petición `POST
   /api/auth/recuperar-contrasena/confirmar` con el email, el código recibido
   y la nueva contraseña.
5. La **Capa de Negocio** (`RecuperacionService.ConfirmarRecuperacionAsync`) valida el
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
  3. El Sistema suma un intento fallido a los códigos vigentes del email. Al tercer
     código equivocado, los invalida (quedan como usados): hay que pedir uno nuevo.
     Esto impide adivinar el código de 6 dígitos probando combinaciones.
  4. El Sistema devuelve un código **400 Bad Request** con el mensaje: "El
     código ingresado es inválido o expiró". El flujo retorna al Paso 1.

* **5b. Nueva contraseña inválida (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 5 la nueva contraseña no cumple la
     política mínima (**RN-02**).
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `PasswordInvalidaException`.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje: "La
     contraseña no cumple los requisitos mínimos". El flujo retorna al Paso 4.

* **1a / 5c. Demasiados pedidos desde el mismo origen (HTTP 429 Too Many Requests):**
  1. El sistema detecta que, desde la misma dirección IP, se superó el máximo de
     pedidos de recuperación (solicitud o confirmación) dentro de la ventana de
     tiempo (por defecto, 5 por minuto).
  2. El Sistema rechaza el pedido sin procesarlo y devuelve un código **429 Too Many
     Requests** con el mensaje: "Demasiados intentos. Espere unos minutos y vuelva a
     intentar". Fin del caso de uso.

* **2a. Rol faltante o inválido (HTTP 400 Bad Request):**
  1. El sistema detecta en el Paso 2 que el JSON no trae el rol, o que no es
     `Cliente`, `Empleado` ni `Admin`.
  2. El Sistema devuelve un código **400 Bad Request** con el error de validación.
     Fin del caso de uso.

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

### 7. DETALLE DE IMPLEMENTACIÓN

| Endpoint | Rol (token JWT) | Controller → Service |
| --- | --- | --- |
| `POST /api/auth/recuperar-contrasena` | — (anónimo) | `AuthPersonalController.SolicitarRecuperacion → RecuperacionService.SolicitarRecuperacionAsync` |
| `POST /api/auth/recuperar-contrasena/confirmar` | — (anónimo) | `AuthPersonalController.ConfirmarRecuperacion → RecuperacionService.ConfirmarRecuperacionAsync` |

* **RN-27:** código de 6 dígitos (generador criptográfico), guardado **hasheado**, vence a los 15 minutos y es de un solo uso (al confirmar se invalidan todos los vigentes del email).
* **RN-28:** si la cuenta es de un empleado, el código se envía a los admins.
* **Email compartido entre roles (CRIT-06):** el email es único dentro de cada rol (RN-01), no entre roles: un empleado puede ser también cliente del local con su mismo email. Por eso la solicitud lleva el `rol` y la cuenta se busca solo en la tabla de ese rol (antes se buscaba en orden cliente → empleado → admin y el empleado nunca podía recuperar su contraseña). El código guarda su rol, así la confirmación cambia la contraseña de esa cuenta e invalida solo sus códigos.
* **Protección contra fuerza bruta (CRIT-04):** `CodigoRecuperacion.IntentosFallidos` cuenta los códigos equivocados; al llegar a `RecuperacionService.MaxIntentosCodigo` (3) los códigos vigentes del email se marcan como usados. Además, los dos endpoints tienen rate limiting por IP (`[EnableRateLimiting("recuperacion")]`, configurado en `RateLimitingExtensions` y en `RateLimiting:Recuperacion` de `appsettings.json`), que limita también cuántos códigos nuevos se pueden pedir.
* El email se envía con `IEmailSender`; en desarrollo `LoggingEmailSender` lo escribe en la consola de la API (ahí se lee el código para la demo).
* Al cambiar la contraseña de un cliente también se le quita el bloqueo por intentos fallidos (RN-03).
* **Colección Bruno:** `bruno/Auth/`.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al solicitar el código (exista o no la cuenta) o al confirmar la nueva contraseña. |
| `400` | Bad Request | Rol faltante o inválido (2a), código inválido/expirado/usado (RN-27), o nueva contraseña inválida (RN-02). |
| `429` | Too Many Requests | Se superó el límite de pedidos de recuperación por IP (flujo 1a/5c). |

### Matriz de trazabilidad CU-25 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `ConfirmarRecuperacionAsync_WithValidCode_UpdatesPassword` | `RecuperarContrasena_WithValidCode_Returns200OK` |
| 2a. Rol faltante o inválido | `400 Bad Request` | — (validación del DTO) | `SolicitarRecuperacion_WithoutValidRole_Returns400BadRequest` |
| 3a. Cuenta inexistente/inactiva | `200 OK` (sin envío) | `SolicitarRecuperacionAsync_WithUnknownAccount_DoesNotSendEmail` | `SolicitarRecuperacion_WithUnknownAccount_Returns200OKWithoutLeakingExistence` |
| 3a. Email solo en otro rol | `200 OK` (sin envío) | `SolicitarRecuperacionAsync_WithEmailOnlyInAnotherRole_DoesNotSendCode` | — |
| Email compartido cliente/empleado (CRIT-06) | `200 OK` | `SolicitarRecuperacionAsync_WithEmailSharedByClienteAndEmpleado_UsesRequestedRole` | `RecuperarContrasena_WithEmailSharedByClienteAndEmpleado_RecoversRequestedRole` |
| 5a. Código inválido/expirado/usado | `400 Bad Request` | `ConfirmarRecuperacionAsync_WithInvalidCode_ThrowsCodigoRecuperacionInvalidoException` | `ConfirmarRecuperacion_WithInvalidCode_Returns400BadRequest` |
| 5b. Contraseña inválida | `400 Bad Request` | `ConfirmarRecuperacionAsync_WithWeakPassword_ThrowsPasswordInvalidaException` | `ConfirmarRecuperacion_WithWeakPassword_Returns400BadRequest` |
| 5a. Tres códigos equivocados | `400 Bad Request` (código invalidado) | `ConfirmarRecuperacionAsync_AfterMaxFailedAttempts_InvalidatesCode` | `ConfirmarRecuperacion_AfterThreeWrongCodes_InvalidatesTheCode` |
| 1a/5c. Límite de pedidos por IP | `429 Too Many Requests` | — (middleware de la API) | `RecuperarContrasena_WhenRateLimitExceeded_Returns429TooManyRequests` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.

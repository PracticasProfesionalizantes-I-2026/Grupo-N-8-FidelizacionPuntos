# Caso de Uso: Registrarse

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-01 (documento y email únicos) y RN-02 (política mínima de
> contraseña) **a implementar**; cada caso borde debe contar con su test unitario e
> integración (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-01 |
| **Nombre** | Registrarse |
| **Actor Principal** | Cliente |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Cliente → crear su cuenta y empezar a acumular puntos; Negocio → captar y fidelizar clientes con datos de contacto válidos; Sistema → mantener la unicidad de documento/email |
| **Disparador (Trigger)** | El cliente solicita crear una cuenta en el sistema |
| **Prioridad / Frecuencia** | Alta; uso frecuente (alta de clientes nuevos) |
| **Reglas de negocio relacionadas** | RN-01 (documento y email únicos por cliente); RN-02 (política mínima de contraseña) |

---

### 1. BREVE DESCRIPCIÓN
Permite que una persona se registre como cliente, creando una cuenta con saldo de
puntos inicial en cero para poder acumular y canjear beneficios.

### 2. PRECONDICIONES
1. El sistema debe estar en funcionamiento y con la Capa de Persistencia accesible.
2. El cliente no debe poseer una cuenta registrada previamente (documento/email sin
   uso previo).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor envía una petición al endpoint `POST /api/clientes/registro` con un JSON
   que contiene los datos del cliente (`nombre`, `documento`, `email`, `password`).
2. La **Capa de Presentación** (`ClientesController.Registrar`) valida que el JSON
   sea estructuralmente correcto y que los campos requeridos estén presentes y no
   vacíos (data annotations `[Required]` sobre `ClienteRegistroCreateDTO`).
3. La **Capa de Negocio** (`ClienteService.RegistrarClienteAsync`) verifica que el
   documento y el email no estén registrados (**RN-01**) y que la contraseña cumpla
   la política mínima de seguridad (**RN-02**).
4. La **Capa de Persistencia** genera un nuevo `Id` (GUID), guarda el registro en la
   tabla `Clientes` y asigna un saldo inicial de puntos en cero.
5. El Sistema devuelve un código **201 Created** con la información resultante
   (ID, nombre, documento, email; sin incluir la contraseña).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 1 el cuerpo de la petición no es un JSON válido.
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Dato obligatorio faltante (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 2 el JSON no incluye `nombre`, `documento`, `email` o `password`.
  2. El Sistema (Capa de Presentación) rechaza la petición por error de validación
     (`ModelState.IsValid == false`).
  3. El Sistema devuelve un código **400 Bad Request** detallando el campo faltante.
     Fin del caso de uso.

* **3a. Documento o email ya registrado (HTTP 409 Conflict):**
  1. El sistema detecta que en el Paso 3 el Sistema detecta que el documento o el email ya pertenecen a
     un cliente existente, violando la regla de negocio **RN-01**.
  2. El Sistema frena la ejecución en la **Capa de Negocio** y lanza la excepción de
     dominio `ClienteDuplicadoException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: "El
     documento/email ingresado ya se encuentra registrado". Fin del caso de uso.

* **3b. Contraseña inválida (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 3 la contraseña no cumple la política mínima de seguridad
     (**RN-02**: longitud y complejidad mínimas).
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `PasswordInvalidaException`.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje: "Los datos
     ingresados son inválidos o están incompletos". Fin del caso de uso.

* **4a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. EL sistema detecta que en el Paso 4 la Capa de Persistencia no puede guardar el registro.
  2. El Sistema interrumpe la operación y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. El actor puede registrarse desde la app web o desde la app móvil; en ambos casos
   el esquema del cuerpo y el resultado (`201 Created`) son idénticos.

### 6. POSTCONDICIONES
1. Se ha creado un nuevo registro persistente en la tabla `Clientes` con ID único
   (GUID) y estado activo.
2. El cliente cuenta con un saldo de puntos inicial en cero, visible en `GET
   /api/clientes/me/saldo` (CU-04).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Confirmación de persistencia exitosa del nuevo recurso Cliente. |
| `400` | Bad Request | Fallo en la validación de esquema/campos, o contraseña que no cumple RN-02. |
| `409` | Conflict | Violación de RN-01: documento o email ya registrados. |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Matriz de trazabilidad CU-01 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `RegistrarClienteAsync_WithValidData_CreatesAndReturnsCliente` | `RegistrarCliente_WithValidData_Returns201Created` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding) | `RegistrarCliente_WithInvalidJson_Returns400BadRequest` |
| 2a. Dato obligatorio faltante | `400 Bad Request` | — (validación de esquema) | `RegistrarCliente_WithMissingRequiredField_Returns400BadRequest` |
| 3a. Documento/email duplicado | `409 Conflict` | `RegistrarClienteAsync_WhenDocumentoOrEmailExists_ThrowsClienteDuplicadoException` | `RegistrarCliente_WhenDuplicateDocumentoOrEmail_Returns409Conflict` |
| 3b. Contraseña inválida | `400 Bad Request` | `RegistrarClienteAsync_WithWeakPassword_ThrowsPasswordInvalidaException` | `RegistrarCliente_WithWeakPassword_Returns400BadRequest` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. Los tests se
> ejecutan con el runner de la suite del proyecto Fidelix.
